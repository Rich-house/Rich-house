#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/.rich-house-common.sh"

RICH_HOUSE_USE_STARTUP_LOG=1

show_sql_failure_diagnostics() {
    emit "SQL diagnostics:"
    emit "Container status: $(container_status)"
    emit "Restart policy: $(container_restart_policy)"
    docker logs --tail 40 "$SQL_CONTAINER_NAME" 2>&1 | tee -a "$STARTUP_LOG"
}

show_service_failure_diagnostics() {
    local service=$1
    local log_file
    local url

    log_file="$(service_log_file "$service")"
    url="$(service_url "$service")"

    emit "$(service_name "$service") diagnostics for $url:"

    if [[ -f "$log_file" ]]; then
        tail -n 40 "$log_file" | tee -a "$STARTUP_LOG"
    else
        emit "No log file found at $log_file"
    fi
}

ensure_docker_ready() {
    if ! docker_available; then
        emit "ERROR: Docker is not installed or not on PATH."
        exit 1
    fi

    if ! docker_daemon_running; then
        emit "ERROR: Docker is installed but the daemon is not running."
        exit 1
    fi

    if ! container_exists; then
        emit "ERROR: Expected SQL Server container '$SQL_CONTAINER_NAME' was not found."
        exit 1
    fi
}

ensure_sql_server() {
    emit "Ensuring SQL Server container restart policy is unless-stopped."
    docker update --restart unless-stopped "$SQL_CONTAINER_NAME" >/dev/null

    if container_running; then
        emit "SQL Server container '$SQL_CONTAINER_NAME' is already running."
    else
        emit "Starting SQL Server container '$SQL_CONTAINER_NAME'."
        docker start "$SQL_CONTAINER_NAME" >/dev/null
    fi

    emit "Waiting for SQL Server to become ready."

    if ! wait_for_sql_ready 120; then
        emit "ERROR: SQL Server did not become ready in time."
        show_sql_failure_diagnostics
        exit 1
    fi

    emit "SQL Server is ready using $SQL_READINESS_METHOD validation."
}

ensure_no_unrelated_port_conflict() {
    local service=$1
    local conflict_pid

    conflict_pid="$(unrelated_listener_pid "$service" || true)"

    if [[ -n "$conflict_pid" ]]; then
        emit "ERROR: Port $(service_port "$service") is occupied by an unrelated process: $(describe_pid "$conflict_pid")"
        exit 1
    fi
}

maybe_reuse_existing_service() {
    local service=$1
    local managed_service_pid
    local listener_pid

    managed_service_pid="$(managed_pid "$service" || true)"

    if [[ -n "$managed_service_pid" ]]; then
        if service_http_ready "$service"; then
            emit "$(service_name "$service") is already running under managed PID $managed_service_pid."
            return 0
        fi

        emit "Managed $(service_name "$service") PID $managed_service_pid exists but is not ready yet. Waiting briefly."
        if wait_for_http_service "$service" 20; then
            emit "$(service_name "$service") became ready without restarting."
            return 0
        fi

        emit "Managed $(service_name "$service") PID $managed_service_pid did not recover. Restarting it."
        terminate_managed_pid "$service" "$managed_service_pid" || true
        cleanup_pid_file "$(service_pid_file "$service")"
    fi

    listener_pid="$(listener_pid_for_service "$service" || true)"

    if [[ -n "$listener_pid" ]]; then
        if service_http_ready "$service"; then
            write_pid_file "$(service_pid_file "$service")" "$listener_pid"
            emit "$(service_name "$service") is already running from this project on port $(service_port "$service"): $(describe_pid "$listener_pid")"
            return 0
        fi

        emit "$(service_name "$service") owns port $(service_port "$service") but is not healthy yet. Waiting briefly."
        if wait_for_http_service "$service" 20; then
            emit "$(service_name "$service") became ready without restarting."
            return 0
        fi

        if [[ "$(process_state "$listener_pid")" == T* ]] || (( $(process_age_seconds "$listener_pid") >= 30 )); then
            emit "Stopping stale $(service_name "$service") listener $(describe_pid "$listener_pid")."
            terminate_unmanaged_pid "$service" "$listener_pid" || true
        else
            emit "ERROR: $(service_name "$service") still owns port $(service_port "$service") but never became healthy: $(describe_pid "$listener_pid")"
            exit 1
        fi
    fi

    cleanup_stale_service_processes "$service"
    return 1
}

start_backend() {
    local default_connection
    local hangfire_connection
    local dotnet_bin
    local pid

    dotnet_bin="$(command -v dotnet || true)"

    if [[ -z "$dotnet_bin" ]]; then
        emit "ERROR: dotnet was not found on PATH."
        exit 1
    fi

    ensure_no_unrelated_port_conflict backend

    if maybe_reuse_existing_service backend; then
        return
    fi

    default_connection="$(runtime_connection_string DefaultConnection)"
    hangfire_connection="$(runtime_connection_string HangfireConnection)"

    emit "Starting API on http://localhost:$BACKEND_PORT."
    printf '\n%s Starting API session\n' "$(timestamp)" >>"$BACKEND_LOG"

    setsid env \
        ASPNETCORE_ENVIRONMENT=Development \
        ASPNETCORE_URLS="http://localhost:$BACKEND_PORT" \
        ConnectionStrings__DefaultConnection="$default_connection" \
        ConnectionStrings__HangfireConnection="$hangfire_connection" \
        bash -c 'cd "$1" && exec "$2" run --no-launch-profile --project "$1/Marketify.csproj"' _ "$BACKEND_DIR" "$dotnet_bin" \
        >>"$BACKEND_LOG" 2>&1 < /dev/null &
    pid=$!

    write_pid_file "$BACKEND_PID_FILE" "$pid"
    emit "API started with managed PID $pid. Waiting for /health."

    if ! wait_for_http_service backend 120; then
        emit "ERROR: API did not become healthy on /health."
        cleanup_pid_file "$BACKEND_PID_FILE"
        show_service_failure_diagnostics backend
        exit 1
    fi

    emit "API is healthy at http://localhost:$BACKEND_PORT/health."
}

start_frontend() {
    local pid

    if [[ ! -x "$FRONTEND_DIR/node_modules/.bin/ng" ]]; then
        emit "ERROR: Angular CLI binary was not found at $FRONTEND_DIR/node_modules/.bin/ng. Install frontend dependencies first."
        exit 1
    fi

    ensure_no_unrelated_port_conflict frontend

    if maybe_reuse_existing_service frontend; then
        return
    fi

    emit "Starting frontend on http://localhost:$FRONTEND_PORT."
    printf '\n%s Starting frontend session\n' "$(timestamp)" >>"$FRONTEND_LOG"

    setsid env \
        CI=1 \
        bash -c 'cd "$1" && exec ./node_modules/.bin/ng serve --host localhost --port '"$FRONTEND_PORT"' --no-open' _ "$FRONTEND_DIR" \
        >>"$FRONTEND_LOG" 2>&1 < /dev/null &
    pid=$!

    write_pid_file "$FRONTEND_PID_FILE" "$pid"
    emit "Frontend started with managed PID $pid. Waiting for HTTP response."

    if ! wait_for_http_service frontend 180; then
        emit "ERROR: Frontend did not start successfully on port $FRONTEND_PORT."
        cleanup_pid_file "$FRONTEND_PID_FILE"
        show_service_failure_diagnostics frontend
        exit 1
    fi

    emit "Frontend is responding at http://localhost:$FRONTEND_PORT."
}

main() {
    ensure_runtime_dirs
    require_paths

    emit "Starting Rich House local development environment from $ROOT_DIR."

    ensure_docker_ready
    ensure_sql_server
    start_backend
    start_frontend

    printf 'SQL Server: running\nAPI: http://localhost:%s\nWebsite: http://localhost:%s\n' "$BACKEND_PORT" "$FRONTEND_PORT" | tee -a "$STARTUP_LOG"
}

main "$@"
