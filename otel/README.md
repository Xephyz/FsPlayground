# Example OTel project
**From: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example**

### Running:
1. Start Aspire Dashboard container using docker

```sh
docker run --rm -it \
-p 18888:18888 \
-p 4317:18889 \
--name aspire-dashboard \
mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

2. Run project
```sh
dotnet run
```

3. Make calls to create OTel data
```sh
curl -k http://localhost:7275
```

4. Go to the dashboard url from step 2 to see logs

