# Local Development

Start everything:

```bash
cd "<PROJECT_ROOT>"
./start-rich-house.sh
```

Open:

`http://localhost:4200`

Check status:

```bash
./status-rich-house.sh
```

Stop app:

```bash
./stop-rich-house.sh
```

Stop app and database:

```bash
./stop-rich-house.sh --include-db
```

## Notes

- SQL Server now restarts automatically after laptop reboot because the existing `marketify-sql` container uses Docker restart policy `unless-stopped`.
- The startup script waits for SQL Server to be truly ready before launching the API.
- Logs are written to:
  - `<PROJECT_ROOT>/.logs/startup.log`
  - `<PROJECT_ROOT>/.logs/backend.log`
  - `<PROJECT_ROOT>/.logs/frontend.log`
- PID files are stored in `<PROJECT_ROOT>/.pids/`.

## Common Recovery Commands

```bash
./status-rich-house.sh
./stop-rich-house.sh
./start-rich-house.sh
```

If you also want to stop SQL Server:

```bash
./stop-rich-house.sh --include-db
```

## Safe Docker Inspection

Check the SQL Server container state:

```bash
docker ps -a --filter name=marketify-sql
```

Inspect recent SQL Server logs safely:

```bash
docker logs --tail 100 marketify-sql
```

Never print or share the `sa` password. The scripts read the existing local credentials without echoing them.
