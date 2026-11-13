# Deploying InventoryHub to AWS

Main target: **Amazon ECS with Fargate** (serverless containers), image in
**Amazon ECR**, database on **Amazon RDS for PostgreSQL**.
Simple alternative: **AWS Elastic Beanstalk** (see the last section).

The app reads its database settings from environment variables, so no code
change is needed for the cloud: set `DatabaseProvider=Npgsql` and provide
the `ConnectionStrings__PostgresConnection` value as a secret.

---

## 1. Push the image to ECR

```bash
AWS_REGION=us-east-1
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)

aws ecr create-repository --repository-name inventoryhub --region $AWS_REGION

aws ecr get-login-password --region $AWS_REGION \
  | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

docker build -t inventoryhub:1.0.0 .
docker tag inventoryhub:1.0.0 $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/inventoryhub:1.0.0
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/inventoryhub:1.0.0
```

## 2. Store the database connection string as a secret

Create an RDS for PostgreSQL instance first (or use any reachable
PostgreSQL), then:

```bash
aws secretsmanager create-secret \
  --name inventoryhub/postgres-connection \
  --secret-string "Host=<rds-endpoint>;Port=5432;Database=inventoryhub;Username=<user>;Password=<password>;SSL Mode=Require"
```

## 3. Register the task definition and create the ECS service

Edit `aws/ecs-task-definition.json`: replace `<AWS_ACCOUNT_ID>`,
`<AWS_REGION>` and the role ARNs with your real values.

```bash
aws ecs create-cluster --cluster-name inventoryhub-cluster

aws ecs register-task-definition --cli-input-json file://aws/ecs-task-definition.json

aws logs create-log-group --log-group-name /ecs/inventoryhub

aws ecs create-service \
  --cluster inventoryhub-cluster \
  --service-name inventoryhub-service \
  --task-definition inventoryhub \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[<subnet-1>,<subnet-2>],securityGroups=[<sg-id>],assignPublicIp=ENABLED}" \
  --load-balancer "targetGroupArn=<alb-target-group-arn>,containerName=inventoryhub,containerPort=8080"
```

Put an **Application Load Balancer** in front of the service for a stable
public DNS name and health checks against `/api/products`.

Updating later: push a new image tag, register a new task-definition
revision, then `aws ecs update-service --force-new-deployment` — the
`deploy-aws` job in `.github/workflows/ci-cd.yml` automates the push and
the service update.

Required GitHub secrets for that job: `AWS_ACCESS_KEY_ID`,
`AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `ECR_REPOSITORY`, `ECS_CLUSTER`,
`ECS_SERVICE`.

## 4. Simple alternative: Elastic Beanstalk

For a single-instance deployment without containers:

```bash
dotnet publish -c Release -o ./publish
cd publish && zip -r ../inventoryhub-app.zip . && cd ..
eb init -p "dotnet-core-linux" inventoryhub
eb create inventoryhub-env --single
eb deploy
```

Beanstalk runs the published app behind its built-in proxy; set the same
`DatabaseProvider` / connection-string values under
**Configuration → Software → Environment properties** in the console.
