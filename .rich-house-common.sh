#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="$ROOT_DIR/frontend"
LOG_DIR="$ROOT_DIR/.logs"
PID_DIR="$ROOT_DIR/.pids"
STARTUP_LOG="$LOG_DIR/startup.log"
BACKEND_LOG="$LOG_DIR/backend.log"
FRONTEND_LOG="$LOG_DIR/frontend.log"
BACKEND_PID_FILE="$PID_DIR/backend.pid"
FRONTEND_PID_FILE="$PID_DIR/frontend.pid"
SQL_CONTAINER_NAME="marketify-sql"
SQL_PORT=1433
BACKEND_PORT=4000
FRONTEND_PORT=4200

timestamp() {
    date '+%Y-%m-%d %H:%M:%S'
}

emit() {
    local line
    line="$(timestamp) $*"

    if [[ "${RICH_HOUSE_USE_STARTUP_LOG:-0}" == "1" ]]; then
        mkdir -p "$LOG_DIR"
        : >>"$STARTUP_LOG"
        printf '%s\n' "$line" | tee -a "$STARTUP_LOG"
        return
    fi

    printf '%s\n' "$line"
}

require_paths() {
    local missing=0

    for path in "$BACKEND_DIR" "$FRONTEND_DIR"; do
        if [[ ! -e "$path" ]]; then
            emit "ERROR: Missing required path: $path"
            missing=1
        fi
    done

    (( missing == 0 ))
}

ensure_runtime_dirs() {
    mkdir -p "$LOG_DIR" "$PID_DIR"
    : >>"$STARTUP_LOG"
    : >>"$BACKEND_LOG"
    : >>"$FRONTEND_LOG"
}

command_available() {
    command -v "$1" >/dev/null 2>&1
}

docker_available() {
    command_available docker
}

docker_daemon_running() {
    docker info >/dev/null 2>&1
}

container_exists() {
    docker inspect "$SQL_CONTAINER_NAME" >/dev/null 2>&1
}

container_status() {
    docker inspect -f '{{.State.Status}}' "$SQL_CONTAINER_NAME" 2>/dev/null || printf 'missing'
}

container_running() {
    [[ "$(container_status)" == "running" ]]
}

container_restart_policy() {
    docker inspect -f '{{.HostConfig.RestartPolicy.Name}}' "$SQL_CONTAINER_NAME" 2>/dev/null || printf 'unknown'
}

sql_logs_indicate_ready() {
    docker logs --tail 60 "$SQL_CONTAINER_NAME" 2>&1 | grep -Eiq 'SQL Server is now ready for client connections|Recovery is complete'
}

sqlcmd_path_in_container() {
    docker exec "$SQL_CONTAINER_NAME" bash -lc 'command -v /opt/mssql-tools18/bin/sqlcmd || command -v /opt/mssql-tools/bin/sqlcmd || command -v sqlcmd || true' 2>/dev/null | head -n 1
}

tcp_port_open() {
    python3 - "$1" "$2" <<'PY'
import socket
import sys

host = sys.argv[1]
port = int(sys.argv[2])

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
    sock.settimeout(1.0)
    raise SystemExit(0 if sock.connect_ex((host, port)) == 0 else 1)
PY
}

wait_for_sql_ready() {
    local timeout_seconds=${1:-120}
    local deadline=$((SECONDS + timeout_seconds))
    local sqlcmd_path

    SQL_READINESS_METHOD=""

    if ! container_running; then
        return 1
    fi

    sqlcmd_path="$(sqlcmd_path_in_container || true)"

    if [[ -n "$sqlcmd_path" ]]; then
        while (( SECONDS < deadline )); do
            if docker exec "$SQL_CONTAINER_NAME" bash -lc "\"$sqlcmd_path\" -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -Q 'SET NOCOUNT ON; SELECT 1' -b -C -l 2 >/dev/null 2>&1"; then
                SQL_READINESS_METHOD="sqlcmd"
                return 0
            fi

            sleep 2
        done

        return 1
    fi

    while (( SECONDS < deadline )); do
        if container_running && tcp_port_open 127.0.0.1 "$SQL_PORT" && sql_logs_indicate_ready; then
            SQL_READINESS_METHOD="tcp+logs"
            return 0
        fi

        sleep 2
    done

    return 1
}

port_listener_pids() {
    lsof -nP -t -iTCP:"$1" -sTCP:LISTEN 2>/dev/null | sort -u
}

pid_is_running() {
    [[ "$1" =~ ^[0-9]+$ ]] && kill -0 "$1" 2>/dev/null
}

process_cmdline() {
    tr '\0' ' ' <"/proc/$1/cmdline" 2>/dev/null || ps -p "$1" -o command= 2>/dev/null || true
}

process_cwd() {
    readlink -f "/proc/$1/cwd" 2>/dev/null || true
}

process_state() {
    ps -o stat= -p "$1" 2>/dev/null | tr -d ' ' || true
}

process_age_seconds() {
    ps -o etimes= -p "$1" 2>/dev/null | tr -d ' ' || printf '0'
}

process_pgid() {
    ps -o pgid= -p "$1" 2>/dev/null | tr -d ' ' || true
}

path_is_within() {
    local path=$1
    local root=$2

    [[ -n "$path" ]] || return 1

    case "$path" in
        "$root"|"$root"/*) return 0 ;;
        *) return 1 ;;
    esac
}

is_backend_process() {
    local pid=$1
    local cwd
    local cmd

    pid_is_running "$pid" || return 1
    cwd="$(process_cwd "$pid")"
    cmd="$(process_cmdline "$pid")"

    path_is_within "$cwd" "$BACKEND_DIR" && [[ "$cmd" == *dotnet* || "$cmd" == *Marketify* ]]
}

is_frontend_process() {
    local pid=$1
    local cwd
    local cmd

    pid_is_running "$pid" || return 1
    cwd="$(process_cwd "$pid")"
    cmd="$(process_cmdline "$pid")"

    path_is_within "$cwd" "$FRONTEND_DIR" && [[ "$cmd" == *"ng serve"* || "$cmd" == *"/ng"* || "$cmd" == *"@angular"* || "$cmd" == *"angular"* || "$cmd" == *"npm run start"* || "$cmd" == *"vite"* ]]
}

service_matches_pid() {
    local service=$1
    local pid=$2

    case "$service" in
        backend) is_backend_process "$pid" ;;
        frontend) is_frontend_process "$pid" ;;
        *) return 1 ;;
    esac
}

service_name() {
    case "$1" in
        backend) printf 'API' ;;
        frontend) printf 'frontend' ;;
        *) return 1 ;;
    esac
}

service_port() {
    case "$1" in
        backend) printf '%s' "$BACKEND_PORT" ;;
        frontend) printf '%s' "$FRONTEND_PORT" ;;
        *) return 1 ;;
    esac
}

service_url() {
    case "$1" in
        backend) printf 'http://localhost:%s' "$BACKEND_PORT" ;;
        frontend) printf 'http://localhost:%s' "$FRONTEND_PORT" ;;
        *) return 1 ;;
    esac
}

service_pid_file() {
    case "$1" in
        backend) printf '%s' "$BACKEND_PID_FILE" ;;
        frontend) printf '%s' "$FRONTEND_PID_FILE" ;;
        *) return 1 ;;
    esac
}

service_log_file() {
    case "$1" in
        backend) printf '%s' "$BACKEND_LOG" ;;
        frontend) printf '%s' "$FRONTEND_LOG" ;;
        *) return 1 ;;
    esac
}

service_health_url() {
    case "$1" in
        backend) printf 'http://localhost:%s/health' "$BACKEND_PORT" ;;
        frontend) printf 'http://localhost:%s/' "$FRONTEND_PORT" ;;
        *) return 1 ;;
    esac
}

read_pid_file() {
    local file=$1
    local pid

    [[ -f "$file" ]] || return 1
    pid="$(tr -d '[:space:]' <"$file")"
    [[ "$pid" =~ ^[0-9]+$ ]] || return 1
    printf '%s' "$pid"
}

cleanup_pid_file() {
    local file=$1

    if [[ -f "$file" ]]; then
        rm -f "$file"
    fi
}

write_pid_file() {
    local file=$1
    local pid=$2

    printf '%s\n' "$pid" >"$file"
}

managed_pid() {
    local service=$1
    local file
    local pid

    file="$(service_pid_file "$service")"
    pid="$(read_pid_file "$file" 2>/dev/null || true)"

    if [[ -n "$pid" ]] && service_matches_pid "$service" "$pid"; then
        printf '%s' "$pid"
        return 0
    fi

    cleanup_pid_file "$file"
    return 1
}

listener_pid_for_service() {
    local service=$1
    local pid

    while IFS= read -r pid; do
        [[ -n "$pid" ]] || continue
        if service_matches_pid "$service" "$pid"; then
            printf '%s' "$pid"
            return 0
        fi
    done < <(port_listener_pids "$(service_port "$service")")

    return 1
}

unrelated_listener_pid() {
    local service=$1
    local pid

    while IFS= read -r pid; do
        [[ -n "$pid" ]] || continue
        if ! service_matches_pid "$service" "$pid"; then
            printf '%s' "$pid"
            return 0
        fi
    done < <(port_listener_pids "$(service_port "$service")")

    return 1
}

list_service_pids() {
    local service=$1
    local pid_dir
    local pid

    for pid_dir in /proc/[0-9]*; do
        pid="${pid_dir##*/}"
        if service_matches_pid "$service" "$pid"; then
            printf '%s\n' "$pid"
        fi
    done | sort -u
}

pid_listens_on_service_port() {
    local service=$1
    local target_pid=$2
    local pid

    while IFS= read -r pid; do
        [[ "$pid" == "$target_pid" ]] && return 0
    done < <(port_listener_pids "$(service_port "$service")")

    return 1
}

http_status_code() {
    curl --silent --show-error --output /dev/null --write-out '%{http_code}' --max-time 3 "$1" 2>/dev/null || true
}

http_is_backend_healthy() {
    [[ "$(http_status_code "$(service_health_url backend)")" == "200" ]]
}

http_is_frontend_responding() {
    local status
    status="$(http_status_code "$(service_health_url frontend)")"
    [[ "$status" =~ ^(200|301|302|304)$ ]]
}

service_http_ready() {
    case "$1" in
        backend) http_is_backend_healthy ;;
        frontend) http_is_frontend_responding ;;
        *) return 1 ;;
    esac
}

wait_for_http_service() {
    local service=$1
    local timeout_seconds=${2:-120}
    local deadline=$((SECONDS + timeout_seconds))

    while (( SECONDS < deadline )); do
        if service_http_ready "$service"; then
            return 0
        fi

        sleep 2
    done

    return 1
}

wait_for_pid_exit() {
    local pid=$1
    local timeout_seconds=${2:-15}
    local deadline=$((SECONDS + timeout_seconds))

    while (( SECONDS < deadline )); do
        if ! pid_is_running "$pid"; then
            return 0
        fi

        sleep 1
    done

    ! pid_is_running "$pid"
}

terminate_unmanaged_pid() {
    local service=$1
    local pid=$2

    service_matches_pid "$service" "$pid" || return 1
    kill -TERM "$pid" 2>/dev/null || true

    if wait_for_pid_exit "$pid" 10; then
        return 0
    fi

    kill -KILL "$pid" 2>/dev/null || true
    wait_for_pid_exit "$pid" 5
}

terminate_managed_pid() {
    local service=$1
    local pid=$2
    local pgid

    service_matches_pid "$service" "$pid" || return 1
    pgid="$(process_pgid "$pid")"

    if [[ -n "$pgid" ]]; then
        kill -TERM -- "-$pgid" 2>/dev/null || true
    else
        kill -TERM "$pid" 2>/dev/null || true
    fi

    if wait_for_pid_exit "$pid" 15; then
        return 0
    fi

    if [[ -n "$pgid" ]]; then
        kill -KILL -- "-$pgid" 2>/dev/null || true
    else
        kill -KILL "$pid" 2>/dev/null || true
    fi

    wait_for_pid_exit "$pid" 5
}

cleanup_stale_service_processes() {
    local service=$1
    local keep_pid=${2:-}
    local min_age_seconds=${3:-30}
    local pid
    local age
    local state

    while IFS= read -r pid; do
        [[ -n "$pid" ]] || continue

        if [[ -n "$keep_pid" && "$pid" == "$keep_pid" ]]; then
            continue
        fi

        if pid_listens_on_service_port "$service" "$pid"; then
            continue
        fi

        state="$(process_state "$pid")"
        age="$(process_age_seconds "$pid")"

        if [[ "$state" == T* ]] || (( age >= min_age_seconds )); then
            emit "Stopping stale $(service_name "$service") process $pid."
            terminate_unmanaged_pid "$service" "$pid" || true
        fi
    done < <(list_service_pids "$service")
}

describe_pid() {
    local pid=$1
    printf 'pid=%s cwd=%s cmd=%s' "$pid" "$(process_cwd "$pid")" "$(process_cmdline "$pid")"
}

resolve_connection_string() {
    local connection_name=$1

    python3 - "$BACKEND_DIR" "$connection_name" <<'PY'
import json
import os
import pathlib
import sys
import xml.etree.ElementTree as ET

backend_dir = pathlib.Path(sys.argv[1])
connection_name = sys.argv[2]
env_key = f"ConnectionStrings__{connection_name}"
secret_key = f"ConnectionStrings:{connection_name}"

value = os.getenv(env_key)
if value:
    print(value)
    raise SystemExit(0)

csproj = backend_dir / "Marketify.csproj"
if csproj.exists():
    try:
        root = ET.parse(csproj).getroot()
        user_secrets_id = None
        for element in root.iter():
            if element.tag.endswith("UserSecretsId") and element.text:
                user_secrets_id = element.text.strip()
                break

        if user_secrets_id:
            secrets_path = pathlib.Path.home() / ".microsoft" / "usersecrets" / user_secrets_id / "secrets.json"
            if secrets_path.exists():
                data = json.loads(secrets_path.read_text(encoding="utf-8-sig"))
                secret_value = data.get(secret_key)
                if secret_value:
                    print(secret_value)
                    raise SystemExit(0)
    except Exception:
        pass

for file_name in ("appsettings.Development.json", "appsettings.json"):
    config_path = backend_dir / file_name
    if not config_path.exists():
        continue

    try:
        data = json.loads(config_path.read_text(encoding="utf-8-sig"))
    except Exception:
        continue

    connection_strings = data.get("ConnectionStrings") or {}
    config_value = connection_strings.get(connection_name)
    if config_value:
        print(config_value)
        raise SystemExit(0)

raise SystemExit(1)
PY
}

container_sa_password() {
    docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' "$SQL_CONTAINER_NAME" 2>/dev/null | awk -F= '$1 == "MSSQL_SA_PASSWORD" { sub(/^[^=]*=/, "", $0); print; exit }'
}

build_runtime_connection_string() {
    local connection_string=$1
    local runtime_password=$2

    python3 - "$connection_string" "$runtime_password" <<'PY'
import sys

connection_string = sys.argv[1]
runtime_password = sys.argv[2]

raw_segments = []
found_user = False
user_value = ""

for raw_segment in connection_string.split(";"):
    segment = raw_segment.strip()
    if not segment:
        continue

    if "=" not in segment:
        raw_segments.append((None, segment))
        continue

    key, value = segment.split("=", 1)
    normalized_key = key.strip().lower()
    cleaned_value = value.strip()

    if normalized_key in {"user id", "uid"}:
        found_user = True
        user_value = cleaned_value

    raw_segments.append((key.strip(), cleaned_value))

should_override_password = bool(runtime_password) and (
    not found_user or user_value.lower() == "sa"
)

segments = []
found_password = False

for key, value in raw_segments:
    if key is None:
        segments.append((key, value))
        continue

    normalized_key = key.lower()
    cleaned_value = value

    if normalized_key in {"password", "pwd"}:
        found_password = True
        if should_override_password:
            cleaned_value = runtime_password

    segments.append((key, cleaned_value))

if should_override_password and not found_password:
    segments.append(("Password", runtime_password))

parts = []
for key, value in segments:
    if key is None:
        parts.append(value)
    else:
        parts.append(f"{key}={value}")

print(";".join(parts))
PY
}

runtime_connection_string() {
    local connection_name=$1
    local connection_string
    local password

    connection_string="$(resolve_connection_string "$connection_name")"
    password="$(container_sa_password || true)"

    if [[ -n "$password" ]]; then
        build_runtime_connection_string "$connection_string" "$password"
        return
    fi

    printf '%s' "$connection_string"
}
