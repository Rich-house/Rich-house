#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/.rich-house-common.sh"

INCLUDE_DB=0

if [[ "${1:-}" == "--include-db" ]]; then
    INCLUDE_DB=1
elif [[ $# -gt 0 ]]; then
    printf 'Usage: %s [--include-db]\n' "$0"
    exit 1
fi

stop_managed_service() {
    local service=$1
    local file
    local pid

    file="$(service_pid_file "$service")"
    pid="$(read_pid_file "$file" 2>/dev/null || true)"

    if [[ -z "$pid" ]]; then
        printf '%s not managed by script.\n' "$(service_name "$service")"
        return
    fi

    if ! service_matches_pid "$service" "$pid"; then
        printf 'Removing stale %s PID file at %s.\n' "$(service_name "$service")" "$file"
        cleanup_pid_file "$file"
        return
    fi

    printf 'Stopping %s managed PID %s.\n' "$(service_name "$service")" "$pid"
    terminate_managed_pid "$service" "$pid" || true
    cleanup_pid_file "$file"

    if service_matches_pid "$service" "$pid"; then
        printf 'Failed to stop %s cleanly.\n' "$(service_name "$service")"
        exit 1
    fi

    printf '%s stopped.\n' "$(service_name "$service")"
}

main() {
    require_paths
    ensure_runtime_dirs

    stop_managed_service frontend
    stop_managed_service backend

    cleanup_pid_file "$FRONTEND_PID_FILE"
    cleanup_pid_file "$BACKEND_PID_FILE"

    if (( INCLUDE_DB == 1 )); then
        if ! docker_available || ! docker_daemon_running; then
            printf 'Docker is unavailable, so the SQL Server container could not be stopped.\n'
            exit 1
        fi

        if ! container_exists; then
            printf "SQL Server container '%s' was not found.\n" "$SQL_CONTAINER_NAME"
            exit 1
        fi

        if container_running; then
            printf "Stopping SQL Server container '%s'.\n" "$SQL_CONTAINER_NAME"
            docker stop "$SQL_CONTAINER_NAME" >/dev/null
            printf 'SQL Server stopped.\n'
        else
            printf 'SQL Server was already stopped.\n'
        fi
    else
        printf 'SQL Server left running.\n'
    fi
}

main "$@"
