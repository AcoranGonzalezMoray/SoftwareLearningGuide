<h1 align="center">Helm — Kubernetes Package Management</h1>

<p align="center">
  <em>Complete guide on how Helm is used in this project to deploy the three .NET applications (API, OutboxProcessor, Consumer) to a Kubernetes cluster</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Helm-0F1689?style=for-the-badge&logo=helm&logoColor=white" alt="Helm">
  <img src="https://img.shields.io/badge/Kubernetes-326CE5?style=for-the-badge&logo=kubernetes&logoColor=white" alt="Kubernetes">
  <img src="https://img.shields.io/badge/Chart-00B4D8?style=for-the-badge&logo=helm&logoColor=white" alt="Helm Chart">
  <img src="https://img.shields.io/badge/Container-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Container">
</p>

---

## Table of Contents

1. [What is Helm?](#what-is-helm)
2. [Why Helm in this project?](#why-helm-in-this-project)
3. [Helm Chart Structure](#helm-chart-structure)
4. [The Three Charts of the Project](#the-three-charts-of-the-project)
    - [software-guide-api](#software-guide-api)
    - [outbox-processor](#outbox-processor)
    - [consumer](#consumer)
5. [values.yaml — Default Configuration](#valuesyaml--default-configuration)
6. [Templates — Kubernetes Resource Generation](#templates--kubernetes-resource-generation)
    - [ConfigMap](#configmap)
    - [Deployment](#deployment)
    - [Service](#service)
    - [Helpers (_helpers.tpl)](#helpers-helperstpl)
    - [NOTES.txt](#notestxt)
7. [Installation — helm upgrade --install](#installation--helm-upgrade--install)
8. [Emulated Deployment with MiniStack](#emulated-deployment-with-ministack)
9. [Real Deployment on AWS EKS](#real-deployment-on-aws-eks)
10. [Daily Operations](#daily-operations)
11. [Best Practices and Security](#best-practices-and-security)
12. [Related Documentation](#related-documentation)

---

## What is Helm?

**Helm** is the **package manager for Kubernetes**. It allows you to define, install, and upgrade applications in a Kubernetes cluster using **Charts** — packages containing a predefined set of Kubernetes resources.

A **Chart** is a collection of YAML files describing the Kubernetes resources needed to run an application. Instead of writing `Deployment`, `Service`, `ConfigMap`, etc. by hand, Helm generates those resources from **templates** and a **values file** (`values.yaml`).

| Characteristic | Detail |
|----------------|--------|
| **Template Language** | Go Templates |
| **Release Management** | Each installation is a "release" with versioning |
| **Rollback** | `helm rollback` automatically reverts changes |
| **Values** | Full parameterization of the chart |
| **Repositories** | Public charts on [Artifact Hub](https://artifacthub.io) |
| **Versioning** | Each chart has its own version (`Chart.yaml`) |

---

## Why Helm in this project?

This project has **three .NET applications** that need to be deployed to Kubernetes. Helm enables:

1. **Per-application parameterization**: Each chart has its own `values.yaml` with service-specific configuration (image, replicas, resources, database connection).
2. **Reproducible deployments**: A `helm upgrade --install` deploys exactly the same thing to any cluster.
3. **Centralized configuration**: All application configuration lives in `values.yaml`, no loose files.
4. **Terraform integration**: Terraform creates the infrastructure (VPC, EKS, ECR); Helm deploys the applications on top of that infrastructure.
5. **Local emulation**: Charts can be tested against MiniStack without touching real AWS.

### Complete Project Flow

```
┌──────────────────────────────────────────────────────────┐
│  1. Terraform (IaC)                                      │
│     Creates infrastructure: VPC + EKS + ECR              │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────┐
│  2. Build Docker Images                                  │
│     dotnet build → Dockerfile → push to ECR              │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────┐
│  3. Helm (Charts)                                        │
│     helm upgrade --install → Deploys 3 apps             │
│     ├── software-guide-api  → REST API                   │
│     ├── outbox-processor    → Outbox Worker               │
│     └── consumer            → Consumer Worker             │
└──────────────────────────────────────────────────────────┘
```

---

## Helm Chart Structure

Each chart follows this standard structure:

```
deploy/helm/<chart-name>/
├── Chart.yaml              # Chart metadata (name, version, description)
├── values.yaml             # Default values (configuration)
└── templates/
    ├── _helpers.tpl        # Reusable templates (names, labels)
    ├── configmap.yaml      # Application configuration (appsettings.Production.json)
    ├── deployment.yaml     # Pod deployment
    ├── service.yaml        # Network service (ClusterIP / NodePort)
    └── NOTES.txt           # Post-installation instructions
```

---

## The Three Charts of the Project

The project has **one chart per .NET application**:

### software-guide-api

| Field | Value |
|-------|-------|
| **Chart name** | `software-guide-api` |
| **Description** | ASP.NET Core REST API of the project |
| **Kubernetes Resources** | Deployment + Service + ConfigMap |
| **Ports** | 8080 (HTTP) |
| **Probes** | `readinessProbe` and `livenessProbe` (TCP) |
| **Default replicas** | 2 |

### outbox-processor

| Field | Value |
|-------|-------|
| **Chart name** | `outbox-processor` |
| **Description** | Worker for processing the Transactional Outbox |
| **Kubernetes Resources** | Deployment + ConfigMap (Deployment only, no Service — no HTTP listener) |
| **Probes** | None (worker without HTTP listener) |
| **Default replicas** | 1 |

### consumer

| Field | Value |
|-------|-------|
| **Chart name** | `consumer` |
| **Description** | Worker consuming Integration Events (RabbitMQ + SNS/SQS) |
| **Kubernetes Resources** | Deployment + ConfigMap (Deployment only, no Service) |

### Differences Between the Three Charts

| Aspect | API | OutboxProcessor | Consumer |
|--------|-----|-----------------|----------|
| **Service** | ✅ Yes (ClusterIP/NodePort) | ❌ No | ❌ No |
| **Probes** | ✅ readiness + liveness (TCP) | ❌ No | ❌ No |
| **Port** | 8080 | N/A | N/A |
| **ConfigMap** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Deployment** | ✅ Yes | ✅ Yes | ✅ Yes |

> **Why no probes for the workers?** OutboxProcessor and Consumer have no HTTP listener, so there's nothing to `tcpSocket` check against. They scale based on RabbitMQ consumption. If they crash-looped due to an error, they wouldn't auto-recover.

---

## values.yaml — Default Configuration

The `values.yaml` is the **heart of the chart**. It defines all default values that templates use to generate the Kubernetes YAMLs.

### Structure of values.yaml (software-guide-api)

```yaml
# ---- Replicas and image ----
replicaCount: 2
image:
  repository: softwarelearningguide/software-guide-api
  pullPolicy: IfNotPresent
  tag: local

# ---- Service ----
service:
  type: ClusterIP
  port: 80
  targetPort: 8080

# ---- Resources ----
resources:
  requests:
    cpu: 100m
    memory: 256Mi
  limits:
    cpu: 500m
    memory: 512Mi

# ---- Environment variables ----
env:
  aspnetUrls: "http://+:8080"

# ---- Application configuration (→ ConfigMap → appsettings.Production.json) ----
config:
  Logging:
    LogLevel:
      Default: "Information"
      Microsoft.AspNetCore: "Warning"
  ConnectionStrings:
    SoftwareLearningGuide: "Server=localhost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  FeatureManagement:
    FT_ENABLE_ORDER_CONTROLLER: true
    # ... more feature flags ...
  CloudProvidersConfigurations:
    AWS:
      Enabled: false
      Credentials:
        AccessKey: "test"
        AccessSecret: "test"
```

### How Configuration Is Injected

The `config` block becomes a **Kubernetes ConfigMap** that mounts `appsettings.Production.json` inside the container. This works because:

1. `ASPNETCORE_ENVIRONMENT=Production` is passed as an environment variable.
2. .NET automatically looks for `appsettings.Production.json`.
3. The ConfigMap mounts the file at `/app/appsettings.Production.json`.

> **Note:** `appsettings.json` (base) and `appsettings.Development.json` (development) are built into the project. Only `appsettings.Production.json` is generated from `values.yaml`.

### Overriding with --set

You can override any value at deploy time:

```bash
# Change the SQL Server connection
helm upgrade --install api ./deploy/helm/software-guide-api -n slg \
  --set config.ConnectionStrings.SoftwareLearningGuide="Server=10.0.0.50,1433;Database=SoftwareLearningGuide;User Id=sa;Password=Production!2024;"

# Change replica count
helm upgrade --install api ./deploy/helm/software-guide-api -n slg \
  --set replicaCount=5

# Enable AWS (SSM + Cognito) against MiniStack
helm upgrade --install api ./deploy/helm/software-guide-api -n slg \
  --set config.CloudProvidersConfigurations.AWS.Enabled=true \
  --set config.CloudProvidersConfigurations.AWS.SSM.ServiceUrl="http://host.docker.internal:4566"
```

---

## Templates — Kubernetes Resource Generation

### ConfigMap

Generates a **ConfigMap** with the `appsettings.Production.json` file:

```yaml
# templates/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: {{ include "slg.fullname" . }}-config
data:
  appsettings.Production.json: |-
    {{- toPrettyJson .Values.config | nindent 4 }}
```

- `{{ include "slg.fullname" . }}`: generates the resource name (e.g., `software-guide-api-config`).
- `{{- toPrettyJson .Values.config | nindent 4 }}`: converts the `config` block from `values.yaml` to formatted JSON with 4-space indentation.

### Deployment

The `Deployment` defines the **Pod** running the application:

```yaml
# templates/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "slg.fullname" . }}
spec:
  replicas: {{ .Values.replicaCount }}
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 25%
      maxUnavailable: 0
  ...
  template:
    spec:
      containers:
        - name: {{ .Chart.Name }}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
          ports:
            - containerPort: {{ .Values.service.targetPort }}
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            - name: ASPNETCORE_URLS
              value: {{ .Values.env.aspnetUrls | quote }}
          volumeMounts:
            - name: appsettings
              mountPath: /app/appsettings.Production.json
              subPath: appsettings.Production.json
              readOnly: true
          resources:
            {{- toYaml .Values.resources | nindent 12 }}
          readinessProbe:
            tcpSocket:
              port: http
          livenessProbe:
            tcpSocket:
              port: http
      volumes:
        - name: appsettings
          configMap:
            name: {{ include "slg.fullname" . }}-config
```

**Key elements:**

| Element | Description |
|----------|-------------|
| **RollingUpdate** | `maxSurge: 25%`, `maxUnavailable: 0` → deployment without downtime |
| **checksum/config** | SHA256 annotation of the ConfigMap. When `values.config` changes, the hash changes and Kubernetes forces a new rollout |
| **volumeMounts** | Mounts the ConfigMap as a file at `/app/appsettings.Production.json` |
| **readinessProbe** | `tcpSocket` on HTTP port → Kubernetes only routes traffic to ready pods |
| **livenessProbe** | `tcpSocket` on HTTP port → Kubernetes restarts the pod if unresponsive |

> **Why `maxUnavailable: 0`?** Guarantees at least one pod is always available during deployment. The new pod starts before the old one stops.

### Service

The `Service` exposes the API within the cluster:

```yaml
# templates/service.yaml
apiVersion: v1
kind: Service
spec:
  type: {{ .Values.service.type }}
  ports:
    - port: {{ .Values.service.port }}
      targetPort: {{ .Values.service.targetPort }}
  selector:
    {{- include "slg.selectorLabels" . | nindent 4 }}
```

By default it's `ClusterIP` (internal cluster access). To access from outside, use `kubectl port-forward` or change to `NodePort` / `LoadBalancer`.

### Helpers (_helpers.tpl)

Contains **reusable templates** for names and labels:

```yaml
{{- define "slg.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Chart.Name (include "slg.suffix" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
```

The `app.kubernetes.io/*` labels are **standard Kubernetes labels** that enable resource selection with `kubectl`:

```bash
kubectl get pods -l app.kubernetes.io/instance=software-guide-api
```

### NOTES.txt

Instructions shown after chart installation:

```text
Software Guide API has been installed!

To verify:
  kubectl -n slg get pods -l app.kubernetes.io/instance=software-guide-api
  kubectl -n slg get svc software-guide-api

To port-forward for local access:
  kubectl -n slg port-forward svc/software-guide-api 5089:80

Then open: http://localhost:5089/swagger
```

---

## Installation — helm upgrade --install

### Base Command

```bash
helm upgrade --install <release-name> <chart-path> -n <namespace> [flags]
```

| Parameter | Description |
|-----------|-------------|
| `upgrade --install` | Updates if release exists; installs if not |
| `<release-name>` | Logical name of the release (e.g., `software-guide-api`) |
| `<chart-path>` | Path to the chart (`./deploy/helm/software-guide-api`) |
| `-n slg` | Kubernetes namespace |
| `--create-namespace` | Creates namespace if it doesn't exist |

### Full Installation Example (Emulated with MiniStack)

> **Important (ErrImagePull):** `image.repository` must be the **mirror** host that
> MiniStack's k3s pre-configures (`000000000000.dkr.ecr.us-east-1.amazonaws.com`).
> `host.docker.internal:4566` is NOT valid for `image.repository`: there is no mirror for
> that host, so the kubelet tries ECR over HTTPS → `ErrImagePull`. The charts already
> default to the right image (`values.yaml`) and `values-emulated.yaml` sets the config
> that points to `host.docker.internal` (SQL/RabbitMQ/MiniStack).

```bash
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml
```

For all three services:

```bash
# API
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml

# OutboxProcessor
helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  -f ./deploy/helm/outbox-processor/values-emulated.yaml

# Consumer
helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  -f ./deploy/helm/consumer/values-emulated.yaml
```

---

## Emulated Deployment with MiniStack

> **This is the mode for practicing without touching real AWS.**

### Architecture

```
┌──────────────────────────────────────────────────────────┐
│  MINISTACK (localhost:4566)                                │
│  ├── SSM Parameter Store                                  │
│  ├── SNS (topics)                                         │
│  ├── SQS (queues)                                         │
│  └── Cognito (User Pool)                                  │
└──────────────────────────┬───────────────────────────────┘
                           │ localhost:4566
                           ▼
┌──────────────────────────────────────────────────────────┐
│  K3s (MiniStack embedded)                                 │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Namespace: slg                                     │  │
│  │  ├── software-guide-api (Deployment + Service)  │  │
│  │  ├── outbox-processor (Deployment)                 │  │
│  │  └── consumer (Deployment)                         │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────┬───────────────────────────────┘
                           │ host.docker.internal
                           ▼
┌──────────────────────────────────────────────────────────┐
│  HOST (your machine)                                      │
│  ├── SQL Server (port 1433)                               │
│  ├── RabbitMQ (port 5672)                                 │
│  └── MiniStack (port 4566)                                │
└──────────────────────────────────────────────────────────┘
```

### Complete Step-by-Step

```bash
# 1. Start MiniStack and supporting services
docker-compose up -d

# 2. Initialize Terraform against MiniStack
cd deploy/IaC
terraform init
terraform apply -var-file="terraform.minstack.tfvars.example"

# 3. Post-apply (ALWAYS after each apply/recreate): kubeconfig for kubectl,
#    re-seed SSM pointing to the host, and the CoreDNS patch so pods can
#    resolve host.docker.internal
cd ..
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Prepare -AppHost host.docker.internal
kubectl get nodes   # → Should show 1 node Ready

# 5. Build and push images to the emulated ECR
docker build -f SoftwareLearningGuide.Api/Dockerfile -t softwarelearningguide/software-guide-api:local .
docker build -f SoftwareLearningGuide.OutboxProcessor/Dockerfile -t softwarelearningguide/outbox-processor:local .
docker build -f SoftwareLearningGuide.Consumer/Dockerfile -t softwarelearningguide/consumer:local .

docker tag softwarelearningguide/software-guide-api:local localhost:4566/softwarelearningguide-api:latest
docker push localhost:4566/softwarelearningguide-api:latest
# ... push all three ...

# 6. Install charts (image.repository and config already point to the emulator:
#    no --set image.repository needed)
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml

helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  -f ./deploy/helm/outbox-processor/values-emulated.yaml

helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  -f ./deploy/helm/consumer/values-emulated.yaml

# 7. Verify
kubectl -n slg get deploy,pods,svc,cm

# 8. Test the API
kubectl -n slg port-forward svc/software-guide-api 5089:80
curl -X POST http://localhost:5089/api/v1/token -d "username=admin@test.com&password=Test1234!"
```

### Teardown

```bash
# Uninstall the charts
helm uninstall software-guide-api outbox-processor consumer -n slg

# Destroy the infrastructure (including the k3s cluster)
cd deploy/IaC
terraform destroy -var-file="terraform.minstack.tfvars.example" -auto-approve
```

---

## Real Deployment on AWS EKS

> **Only run this when deploying for real. Infrastructure = cost.**

### Flow

1. **Terraform** creates VPC + EKS + ECR on real AWS.
2. **Docker** builds images and `docker push`es to ECR.
3. **Helm** installs charts pointing to ECR images.
4. **AWS services** (RDS, SNS/SQS, Cognito) are managed separately or configured via SSM.

```bash
# 1. Create infrastructure
cp terraform.tfvars.example terraform.tfvars
terraform init
terraform apply -var-file="terraform.tfvars"

# 2. Get kubeconfig
aws eks --region us-east-1 update-kubeconfig --name softwarelearningguide-dev-eks

# 3. Build and push images to ECR
$ACCOUNT = (aws sts get-caller-identity --query Account --output text)
docker build -f SoftwareLearningGuide.Api/Dockerfile -t softwarelearningguide/software-guide-api:local .
# ... push to ECR ...

# 4. Install charts with ECR images
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg \
  --set image.repository="$ACCOUNT.dkr.ecr.us-east-1.amazonaws.com/softwarelearningguide-api" \
  --set image.tag=latest
```

---

## Daily Operations

### Essential Helm Commands

```bash
# List installed releases
helm ls -n slg

# Check release status
helm status software-guide-api -n slg

# See what would change without applying
helm diff upgrade software-guide-api ./deploy/helm/software-guide-api -n slg

# Update (change values or update image)
helm upgrade software-guide-api ./deploy/helm/software-guide-api -n slg

# See generated template (without applying)
helm template software-guide-api ./deploy/helm/software-guide-api -n slg

# Rollback to previous version
helm history software-guide-api -n slg
helm rollback software-guide-api 1 -n slg

# Scale
kubectl -n slg scale deploy/software-guide-api --replicas=5

# View logs
kubectl -n slg logs -l app.kubernetes.io/instance=consumer -f

# View applied ConfigMap
kubectl -n slg get cm software-guide-api-config -o yaml | Select-Object -First 80
```

### Post-Installation Verification

```bash
# Check deployments, pods, services, and configmaps
kubectl -n slg get deploy,pods,svc,cm

# Verify all pods are Running
kubectl -n slg rollout status deploy/software-guide-api --timeout=180s
kubectl -n slg rollout status deploy/outbox-processor --timeout=120s
kubectl -n slg rollout status deploy/consumer --timeout=120s

# Port-forward for local testing
kubectl -n slg port-forward svc/software-guide-api 5089:80
```

---

## Best Practices and Security

### Best Practices Applied

- **ConfigMap immutable by checksum**: The `checksum/config` annotation on the Deployment guarantees that any change in `values.config` triggers an automatic rollout.
- **RollingUpdate without downtime**: `maxSurge: 25%` and `maxUnavailable: 0` ensure progressive updates without service interruption.
- **Probes for the API**: `readinessProbe` and `livenessProbe` via TCP ensure traffic only reaches healthy pods.
- **Resource limits**: Each pod has CPU/memory `requests` and `limits` to prevent resource abuse.
- **Isolated namespace**: Everything lives in the `slg` namespace, making management and teardown easy (`helm uninstall -n slg`).
- **Tagged images**: `image.tag=latest` in dev; specific versions in production.
- **Secrets outside the repo**: In production, secrets go in Kubernetes `Secret` or SSM Parameter Store / Secrets Manager, not in `values.yaml`.

### Security

- **`ASPNETCORE_ENVIRONMENT=Production`**: Charts always deploy in production mode so .NET loads `appsettings.Production.json`.
- **No credentials in charts**: Access values are injected from SSM or Secrets Manager.
- **Network Policies** (not yet implemented): Adding network policies to restrict pod-to-pod traffic is recommended.
- **RBAC**: The EKS cluster uses Access Entries (`authentication_mode = API_AND_CONFIG_MAP`) instead of manually editing `aws-auth`.

---

## Related Documentation

| Topic | Document |
|-------|----------|
| **Terraform (IaC)** | [`README.Terraform.en.md`](README.Terraform.en.md) |
| **MiniStack (local emulator)** | [`README.Ministack.en.md`](README.Ministack.en.md) |
| **AWS general (service catalog)** | [`README.AWS.en.md`](README.AWS.en.md) |
| **Full deployment (IaC + Helm)** | [`README.Deploy.en.md`](README.Deploy.en.md) |
| **API configuration** | [`SoftwareLearningGuide.Api/README.Api.en.md`](../SoftwareLearningGuide.Api/README.Api.en.md) |
| **Docker Compose local** | [`README.en.md`](../../../README.en.md) |
| **Feature Management** | [`SoftwareLearningGuide.Api/README.FeatureManagement.en.md`](../SoftwareLearningGuide.Api/README.FeatureManagement.en.md) |
| **Observability** | [`SoftwareLearningGuide.Api/README.Observability.en.md`](../SoftwareLearningGuide.Api/README.Observability.en.md) |

---

**Happy Helm deployment!** 🚀
