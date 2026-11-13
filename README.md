# InventoryHub — Product Inventory Manager

A complete full-stack **C# / ASP.NET Core 8** application: a REST Web API
with **Entity Framework Core**, a single-page frontend served by the same
app, and ready-to-use deployment configuration for **Docker**,
**Kubernetes**, **Azure** and **AWS** — the journey from local code to the
cloud with no code changes, only configuration.

**Author:** Abel Haile Magicho — [github.com/kembAB](https://github.com/kembAB)

## Architecture

```
Browser ──► wwwroot/index.html  (single-page app: HTML + CSS + JavaScript)
                │  fetch()
                ▼
        ASP.NET Core 8 Web API  (ProductsController, /api/products)
                │
                ▼
        Entity Framework Core   (AppDbContext, Product entity)
                │
        ┌───────┴────────────┐
        ▼                    ▼
   SQLite (local)     PostgreSQL (Docker / cloud)
   DatabaseProvider=Sqlite     DatabaseProvider=Npgsql
```

The database provider is selected **by configuration only**
(`DatabaseProvider` in `appsettings.json` or an environment variable), so
the same build runs on SQLite locally and on PostgreSQL in containers and
in the cloud.

### API endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get one product |
| GET | `/api/products/low-stock?threshold=5` | Products below a stock threshold |
| POST | `/api/products` | Create a product (SKU must be unique) |
| PUT | `/api/products/{id}` | Full update |
| PATCH | `/api/products/{id}/stock` | Update stock quantity only |
| DELETE | `/api/products/{id}` | Delete a product |

Validation is automatic via data annotations: a missing name, a price below
0.01 or a negative stock quantity returns **HTTP 400**; a duplicate SKU
returns **HTTP 409**. Swagger UI is available at `/swagger`.
On first run the database is created and seeded with 6 sample products.
The frontend (in `wwwroot`) lists, adds, edits, updates stock for and
deletes products, and highlights low-stock and out-of-stock rows.

## Run it locally

Requires the .NET 8 SDK.

```bash
dotnet restore
dotnet build -c Release
dotnet run
# or run the built DLL:
dotnet bin/Release/net8.0/InventoryHub.dll --urls http://localhost:5000
```

Open http://localhost:5000 for the app, http://localhost:5000/swagger for
the API documentation. The SQLite database file `inventoryhub.db` is
created next to the project on first run; delete it to reseed.

Quick API check:

```bash
curl http://localhost:5000/api/products
curl -X POST http://localhost:5000/api/products -H "Content-Type: application/json" \
  -d '{"name":"Webcam HD 1080p","sku":"WC-6001","category":"Peripherals","price":59.99,"stockQuantity":3}'
curl "http://localhost:5000/api/products/low-stock?threshold=5"
```

See `output.txt` in this repo for a real captured run of exactly these calls.

## Run it with Docker

Single container (SQLite inside the container):

```bash
docker build -t inventoryhub .
docker run -p 8080:8080 inventoryhub
```

Full stack with PostgreSQL (the configuration-only database switch):

```bash
docker compose up --build
```

The compose file starts the app plus PostgreSQL 16 and sets
`DatabaseProvider=Npgsql` with the matching connection string — no code
was changed. App: http://localhost:8080

## Run it on Kubernetes

Manifests in `k8s/`: Deployment (2 replicas, probes, resource limits),
Service, Ingress (NGINX), ConfigMap and a Secret example.

```bash
# 1. Push the image to your registry and update image: in k8s/deployment.yaml
# 2. Create the real secret (never commit credentials)
kubectl create secret generic inventoryhub-secret \
  --from-literal=PostgresConnection="Host=<db-host>;Port=5432;Database=inventoryhub;Username=<user>;Password=<password>"
# 3. Apply
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml
kubectl get pods,svc,ingress
```

## Deploy to Azure

Full guide: **[azure-deploy.md](azure-deploy.md)** — Azure Container
Registry build, then **Azure Container Apps** (simplest) or **AKS** using
the `k8s/` manifests, with an Azure Database for PostgreSQL backend.

## Deploy to AWS

Full guide: **[aws/aws-deploy.md](aws/aws-deploy.md)** — ECR push and an
**ECS Fargate** service using `aws/ecs-task-definition.json`, with RDS for
PostgreSQL; **Elastic Beanstalk** is documented as the simple alternative.

## CI/CD

`.github/workflows/ci-cd.yml` runs on every push and pull request:
restore, build, test (automatically, when a test project exists in the
repo), and a Docker image build. On pushes to `main` it also deploys —
one job updates the Azure Container App from ACR, another pushes to ECR
and forces a new ECS deployment. Cloud credentials are read from GitHub
secrets; see the workflow file header for the exact secret names.

## Project layout

```
Program.cs                  Startup: services, DB provider switch, seeding, pipeline
Models/Product.cs           Product entity + validation rules
Models/StockUpdateRequest.cs  Body for the stock-only PATCH endpoint
Data/AppDbContext.cs        EF Core context, unique SKU index, seed data
Controllers/ProductsController.cs  Full CRUD + low-stock API
wwwroot/index.html          Single-page frontend served by the API app
appsettings.json            Local config: SQLite default, PostgreSQL string ready
Dockerfile                  Multi-stage build: SDK → ASP.NET runtime
docker-compose.yml          App + PostgreSQL stack
k8s/                        Kubernetes manifests
azure-deploy.md             Azure deployment guide (ACR, Container Apps, AKS)
aws/                        ECS task definition + AWS deployment guide (ECR, Fargate, Beanstalk)
.github/workflows/ci-cd.yml GitHub Actions pipeline with Azure + AWS deploy jobs
output.txt                  Real captured run: build + API calls against the app
inventoryhub-dotnet-explainer.pdf  Full project explainer (PDF)
```

## Honest limitations

- The local profile uses `EnsureCreated` + seed-on-start for zero setup;
  production systems should use EF Core migrations instead.
- The cloud guides are configuration-ready and were written against the
  real Azure/AWS CLIs, but no live cloud account was used to execute them
  from this repo — run the steps with your own subscription/account.
- No authentication is built in; add ASP.NET Core Identity or JWT before
  exposing the API publicly.

## Author

**Abel Haile Magicho** — Senior Software / AI Engineer, Houston TX.
GitHub: [github.com/kembAB](https://github.com/kembAB)
