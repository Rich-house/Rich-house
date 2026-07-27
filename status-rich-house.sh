#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/.rich-house-common.sh"

print_service_status() {
    local service=$1
    local managed_service_pid
    local listener_pid
    local health_url
    local health_code

    managed_service_pid="$(managed_pid "$service" || true)"
    listener_pid="$(listener_pid_for_service "$service" || true)"
    health_url="$(service_health_url "$service")"
    health_code="$(http_status_code "$health_url")"

    if [[ -n "$managed_service_pid" ]]; then
        printf '%s process: managed pid %s\n' "$(service_name "$service")" "$managed_service_pid"
    elif [[ -n "$listener_pid" ]]; then
        printf '%s process: project listener %s\n' "$(service_name "$service")" "$(describe_pid "$listener_pid")"
    else
        printf '%s process: not running\n' "$(service_name "$service")"
    fi

    if [[ -n "$listener_pid" ]]; then
        printf 'Port %s: listening by %s\n' "$(service_port "$service")" "$(describe_pid "$listener_pid")"
    else
        printf 'Port %s: not listening\n' "$(service_port "$service")"
    fi

    if [[ "$service" == "backend" && "$health_code" == "200" ]]; then
        printf 'API health: 200 OK (%s)\n' "$health_url"
    elif [[ "$service" == "frontend" && "$health_code" =~ ^(200|301|302|304)$ ]]; then
        printf 'Frontend HTTP: %s (%s)\n' "$health_code" "$health_url"
    else
        printf '%s health: unavailable (%s)\n' "$(service_name "$service")" "$health_url"
    fi
}

main() {
    require_paths
    ensure_runtime_dirs

    if ! docker_available; then
        printf 'Docker daemon: unavailable (docker command not found)\n'
    elif docker_daemon_running; then
        printf 'Docker daemon: running\n'
    else
        printf 'Docker daemon: stopped\n'
    fi

    if docker_available && docker_daemon_running && container_exists; then
        printf "marketify-sql: %s (restart=%s)\n" "$(container_status)" "$(container_restart_policy)"
    else
        printf 'marketify-sql: unavailable\n'
    fi

    if tcp_port_open 127.0.0.1 "$SQL_PORT"; then
        printf 'Port 1433: listening\n'
    else
        printf 'Port 1433: not listening\n'
    fi

    print_service_status backend
    print_service_status frontend

    printf 'Logs:\n'
    printf '  startup: %s\n' "$STARTUP_LOG"
    printf '  backend: %s\n' "$BACKEND_LOG"
    printf '  frontend: %s\n' "$FRONTEND_LOG"
}

main "$@"
