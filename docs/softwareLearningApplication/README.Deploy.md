<h1 align="center">Deployment — Despliegue (IaC + Helm)</h1>

<p align="center">
  <em>Guía completa del despliegue de los tres servicios del proyecto y flujo completo <strong>API → OutboxProcessor → Consumer</strong>. Dos escenarios: emulación local con MiniStack y despliegue real en AWS</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Terraform-7B42BC?style=for-the-badge&logo=terraform&logoColor=white" alt="Terraform">
  <img src="https://img.shields.io/badge/Helm-0F1689?style=for-the-badge&logo=helm&logoColor=white" alt="Helm">
  <img src="https://img.shields.io/badge/AWS-FF9900?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS">
  <img src="https://img.shields.io/badge/MiniStack-45B7D1?style=for-the-badge&logo=docker&logoColor=white" alt="MiniStack">
  <img src="https://img.shields.io/badge/Kubernetes-326CE5?style=for-the-badge&logo=kubernetes&logoColor=white" alt="Kubernetes">
</p>

---

## Tabla de Contenidos

1. [Arquitectura de Despliegue](#arquitectura-de-despliegue)
    - [Componentes](#componentes)
    - [Estructura de Carpetas](#estructura-de-carpetas)
2. [Requisitos Previos](#requisitos-previos)
3. [Flujo de Emulación Local (MiniStack)](#flujo-de-emulación-local-ministack)
    - [Paso 0 — Levantar Infraestructura](#paso-0--levantar-infraestructura)
    - [Paso 1 — AWS CLI Apuntando a MiniStack](#paso-1--aws-cli-apuntando-a-ministack)
    - [Paso 2 — Ejecutar las Apps en Local](#paso-2--ejecutar-las-apps-en-local)
    - [Paso 3 — Probar el Flujo Completo](#paso-3--probar-el-flujo-completo)
    - [Paso 4 — Observabilidad](#paso-4--observabilidad)
    - [Paso 5 — Flujo Emulado: Terraform + Helm contra MiniStack](#paso-5--flujo-emulado-terraform--helm-contra-ministack)
    - [Paso 6 — Operar MiniStack con AWS CLI](#paso-6--operar-ministack-con-aws-cli)
    - [Paso 7 — Teardown (Emulación Local)](#paso-7--teardown-emulación-local)
4. [Terraform: Sin Credenciales, Con Credenciales o contra MiniStack](#terraform-sin-credenciales-con-credenciales-o-contra-ministack)
5. [Despliegue Real en AWS (Referencia)](#despliegue-real-en-aws-referencia)
6. [Operación Diaria (AWS Real)](#operación-diaria-aws-real)
7. [Troubleshooting](#troubleshooting)
8. [Seguridad y Buenas Prácticas](#seguridad-y-buenas-prácticas)
9. [Documentación Relacionada](#documentación-relacionada)

---

## Arquitectura de Despliegue

```
 Emulación local (sin AWS real)                         AWS real (referencia)
 -------------------------------                       -----------------------
                                               ┌──────────────────────────────────────┐
┌───────────────────────────────┐            │  AWS: VPC + EKS                      │
│ TU MÁQUINA                    │            │                                      │
│ ┌────────────┐ ┌─────────────┐│            │  ┌───────────────┐                   │
│ │  API       │ │ Outbox +    ││            │  │ software-guide│                   │
│ │  (dotnet)  │ │ Consumer    ││            │  │ -api / outbox │                   │
│ └─────┬──────┘ │ (dotnet)    ││            │  │ / consumer    │  Deployments      │
│       │        └──────┬──────┘│            │  └───┬─────┬─────┘                   │
│       │                │      │            │      │     │                         │
│   Docker Compose       │      │            │   RDS / AmazonMQ / SNS/SQS / Cognito │
│  ┌─────────────────────┴────┐ │            │  (servicios AWS gestionados)         │
│  │ SQL ◄─ RabbitMQ ◄─ Redis│ │            └──────────────────────────────────────┘
│  │ Flagsmith ◄─ MiniStack  │  │
│  │ (SSM/SNS/SQS/Cognito)    │ │
│  └──────────────────────────┘ │
└───────────────────────────────┘

Configuración:  local       → appsettings.Development.json (ya apunta a localhost y a MiniStack)
                Kubernetes → ConfigMap → /app/appsettings.Production.json (un chart por servicio)
Instalación K8s: helm upgrade --install <release> deploy/helm/<chart>
```

### Componentes

| Componente | Chart / Módulo Terraform | Rol |
|------------|--------------------------|-----|
| **API REST** | `helm/software-guide-api` | Expone los endpoints, persiste en SQL Server, escribe la tabla de Outbox en la misma transacción |
| **OutboxProcessor** | `helm/outbox-processor` | Worker que escanea la tabla `OutboxMessage` y publica los Integration Events a RabbitMQ (y SNS/SQS si AWS está activo) |
| **Consumer** | `helm/consumer` | Worker que consume los Integration Events de RabbitMQ (y del bus SNS/SQS) y ejecuta la lógica de negocio |
| **Red (VPC)** | `IaC/modules/vpc` | VPC multi-AZ, subredes pública/privada, NAT, Internet Gateway, Flow Logs |
| **Imágenes (ECR)** | `IaC/modules/ecr` | Un repositorio por imagen, con política de lifecycle y escaneo |
| **Kubernetes (EKS)** | `IaC/modules/eks` | Clúster EKS, managed node group, add-ons, Access Entries, OIDC (IRSA) |

### Estructura de Carpetas

```
deploy/
├── README.Deploy.md                   ← esta guía
├── IaC/                                # Terraform (VPC, ECR, EKS)
│   ├── main.tf                         # Providers + orquestación de módulos
│   ├── variables.tf                    # Todas las variables parametrizadas
│   ├── outputs.tf                      # kubeconfig, URLs ECR, ids de red, OIDC...
│   ├── versions.tf                     # Versión de Terraform/providers + backend
│   ├── terraform.tfvars.example        # Valores de ejemplo (copia a terraform.tfvars)
│   ├── terraform.minstack.tfvars.example # Valores para emulación contra MiniStack
│   └── modules/
│       ├── vpc/     (main/variables/outputs)   # VPC, subredes, NAT, Flow Logs
│       ├── ecr/     (main/variables/outputs)   # Repos ECR + lifecycle policy
│       └── eks/     (main/variables/outputs)   # Clúster, node group, add-ons, OIDC
├── helm/
│   ├── software-guide-api/             # Chart de la API (Deployment + Service)
│   │   ├── Chart.yaml
│   │   ├── values.yaml                 # imagen, réplicas, resources, config completa
│   │   └── templates/
│   │       ├── _helpers.tpl            # helpers de nombres/labels (reutilizables)
│   │       ├── configmap.yaml          # appsettings.Production.json desde values
│   │       ├── deployment.yaml         # Deployment + probes + resources + env
│   │       ├── service.yaml            # Service (ClusterIP / NodePort)
│   │       └── NOTES.txt               # instrucciones post-instalación
│   ├── outbox-processor/               # Chart del worker (solo Deployment)
│   │   ├── Chart.yaml
│   │   ├── values.yaml
│   │   └── templates/ (configmap/deployment/helpers/NOTES)
│   └── consumer/                       # Chart del worker (solo Deployment)
│       ├── Chart.yaml
│       ├── values.yaml
│       └── templates/ (configmap/deployment/helpers/NOTES)
└── scripts/
    └── ministack.ps1                   # AWS CLI → MiniStack: perfil, verify, reseed SSM
```

Los **Dockerfiles** de cada servicio viven en la carpeta de cada proyecto:
`SoftwareLearningGuide.Api/Dockerfile`, `SoftwareLearningGuide.OutboxProcessor/Dockerfile`
(existente), `SoftwareLearningGuide.Consumer/Dockerfile`.

---

## Requisitos Previos

| Herramienta | Versión mínima | Instalación (Windows) |
|-------------|----------------|-----------------------|
| Docker Desktop | 4.x | `winget install Docker.DockerDesktop` (habilitar el engine en WSL 2) |
| .NET SDK | 10.x | `winget install Microsoft.DotNet.SDK.10` |
| AWS CLI | 2.x | `winget install Amazon.AWSCLI` (para operar MiniStack) |
| Terraform | 1.5+ | `winget install Hashicorp.Terraform` (solo IaC) |

**Para el despliegue en un clúster de Kubernetes** (la opción "completa emulada" contra el EKS de MiniStack y el despliegue real en AWS): Helm 3.11+ (`winget install Helm.Helm`) y kubectl 1.28+ (`winget install Kubernetes.kubectl`). El flujo rápido con `dotnet run` no los usa.

Verificación rápida:

```powershell
docker --version
dotnet --version
aws --version
```

> **Emulación local = MiniStack, no minikube.** La infraestructura de apoyo (SQL, RabbitMQ, Redis, Flagsmith, Aspire y el emulador AWS) corre en contenedores **en el host** (`localhost`), y las tres aplicaciones corren con `dotnet run`. La AWS CLI se usa para *operar* MiniStack (SSM/SNS/SQS/Cognito), nunca contra AWS real.

---

## Flujo de Emulación Local (MiniStack)

> **Objetivo: correr el sistema completo sin coste y sin AWS real.** En modo `Development` las tres apps **ya activan MiniStack por defecto**: la configuración de desarrollo apunta a `http://localhost:4566` para SSM/Messaging/Cognito, a `localhost` para SQL/RabbitMQ y a `http://localhost:4317` para OTLP.

### Paso 0 — Levantar la Infraestructura (MiniStack incluido)

Desde la **raíz del repositorio**:

```powershell
docker-compose up -d
```

Esto levanta (puertos del host):

| Servicio | Puerto | Uso |
|----------|--------|-----|
| SQL Server | `1433` | Base de datos principal (API + OutboxProcessor) |
| RabbitMQ | `5672` / `15672` | Broker de mensajes (management: `guest/guest`) |
| MiniStack | `4566` | Emulador AWS (SSM, SNS, SQS, Cognito) |
| Redis | `6379` | Backend de estado de MiniStack |
| Flagsmith | `8000` | Feature management (usado por la API en dev) |
| Aspire Dashboard | `18888` / `4317` | Observabilidad OTLP |

MiniStack **siembra SSM y Cognito al arrancar** (los scripts `scripts/ministack/init-ssm.sh` y `cognito-init.sh` van montados en `/docker-entrypoint-initaws.d/ready.d`): parámetros SSM con valores `localhost` y un user pool de Cognito con `admin@test.com` / `Test1234!` (grupo `admin`) y `user@test.com` / `Test1234!` (grupo `normal`).

```powershell
docker-compose ps
```

### Paso 1 — AWS CLI Apuntando a MiniStack

**Sí: hay que tener AWS CLI y apuntarla al endpoint de MiniStack.** MiniStack acepta cualquier credencial; por convenio se usan `test`/`test` en `us-east-1`.

Hay un helper que hace todo (perfil, verificación y re-siembra):

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action All
```

Equivale a (a mano):

```powershell
# 1) Perfil de AWS CLI que apunta a MiniStack
aws configure set aws_access_key_id     test        --profile ministack
aws configure set aws_secret_access_key test        --profile ministack
aws configure set region                us-east-1   --profile ministack
aws configure set endpoint_url          http://localhost:4566 --profile ministack   # AWS CLI v2

# 2) A partir de ahora, SIEMPRE con --profile ministack --endpoint-url http://localhost:4566
aws --profile ministack --endpoint-url http://localhost:4566 ssm get-parameters-by-path `
  --path "/softwarelearningguide/" --recursive --query "Parameters[].{Name:Name,Value:Value}" `
  --output table
```

> Con AWS CLI v1 no existe `endpoint_url` por perfil: pasa `--endpoint-url` en cada comando (el helper lo hace por ti). Las credenciales `test`/`test` solo valen contra MiniStack.

Verifica que la siembra respondió (SSM, Cognito, SNS/SQS):

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Verify
```

Si algo quedó vacío o reiniciaste MiniStack sin persistencia:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Reseed
```

> Los parámetros de **Cognito** (`UserPoolId`/`ClientId`) son dinámicos y los genera `cognito-init.sh`. Si tu pool desapareció, re-ejecútalo dentro del contenedor: `docker compose exec ministack sh /docker-entrypoint-initaws.d/ready.d/cognito-init.sh`

### Paso 2 — Ejecutar las Apps en Local

Arranca los tres proyectos en modo `Development` (su configuración ya usa **SQL + RabbitMQ + MiniStack + OTLP** en localhost). Desde la **raíz del repo**:

```powershell
dotnet run --project SoftwareLearningGuide.Api --launch-profile http
dotnet run --project SoftwareLearningGuide.OutboxProcessor --environment Development
dotnet run --project SoftwareLearningGuide.Consumer --environment Development
```

> El `--environment Development` (o `$env:ASPNETCORE_ENVIRONMENT="Development"`) hace que los workers carguen su `appsettings.Development.json`, que es el que ya apunta a SQL/RabbitMQ/MiniStack en `localhost`.

> En Visual Studio puedes lanzar los tres a la vez con el perfil de arranque **"Api + Outbox + Consumer"** (`SoftwareLearningGuide.slnLaunch`).

La API arranca en `http://localhost:5089` (Swagger en `/swagger`). Los workers escriben en su consola y publican telemetría al Aspire Dashboard.

### Paso 3 — Probar el Flujo Completo

1) Con **Cognito activo** (modo dev) los endpoints requieren JWT. Dale a la utilidad de ayuda de la API tu usuario/contraseña y obtén el `access_token`:

```powershell
$TOKEN = (curl.exe -s -X POST http://localhost:5089/api/v1/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "username=admin@test.com&password=Test1234!" | ConvertFrom-Json).access_token
```

> También puedes obtenerlo con la AWS CLI apuntando a MiniStack (ver Paso 6).

2) Crea un producto (esto escribe `Product` + `OutboxMessage` en la **misma transacción**):

```powershell
curl.exe -s -X POST http://localhost:5089/api/v1/product `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $TOKEN" `
  -d '{"name":"Laptop XP","description":"Gaming laptop","price":1200.00,"currency":"USD","stockQuantity":10}'
```

3) El **OutboxProcessor** detecta el mensaje pendiente (sondea cada ~5 s) y lo publica en RabbitMQ. Mira **su consola** y deberías ver la publicación del `ProductCreatedIntegrationEvent`.

4) El **Consumer** recibe el evento de RabbitMQ (y su bus SNS/SQS de MiniStack) y ejecuta la lógica. Mira **su consola**:

```
[ProductCreatedConsumer] Recibido ProductCreatedIntegrationEvent
  ProductId=... | Name=Laptop XP | Price=1200.00 | Currency=USD
```

5) Verifica en la UI de RabbitMQ los exchanges/colas de MassTransit: `http://localhost:15672` (`guest` / `guest`) → *Exchanges* / *Queues*.

> Si un endpoint responde `404`, comprueba que el feature flag `FT_*` correspondiente está activo en Flagsmith (`http://localhost:8000`); en dev la API lee sus flags del Flagsmith local.

### Paso 4 — Observabilidad (opcional)

Sin tocar nada: las apps en dev envían OTLP a `http://localhost:4317` (puerto del Aspire Dashboard). Abre `http://localhost:18888` y verás trazas, métricas y logs de los tres procesos.

### Paso 5 — Flujo Emulado: Terraform + Helm contra MiniStack (opcional)

> Hasta aquí las apps corren con `dotnet run`. Esta opción reproduce el **despliegue real de producción** pero contra MiniStack: **Terraform crea VPC/ECR/EKS en el emulador** (su EKS levanta un clúster **k3s/k3d real embebido en Docker**) y **Helm instala los charts en ese clúster**. Nada toca AWS real.

1) Crear la infraestructura emulada (estado local; MiniStack debe estar corriendo):

```powershell
cd deploy/IaC
terraform init
terraform plan  -var-file="terraform.minstack.tfvars.example"
terraform apply -var-file="terraform.minstack.tfvars.example"
```

> Con este tfvars el provider apunta **TODOS** los servicios a `http://localhost:4566` (credenciales `test`/`test`, sin llamadas a STS) y se desactivan las partes que el emulador no soporta: NAT, flow logs, node group, add-ons, Access Entries y OIDC. La primera vez, MiniStack **descarga y arranca k3d/k3s** al crear el clúster (puede tardar un par de minutos).

Comprueba los recursos emulados:

```powershell
aws --profile ministack --endpoint-url http://localhost:4566 ecr describe-repositories
aws --profile ministack --endpoint-url http://localhost:4566 eks describe-cluster --name softwarelearningguide-dev-eks
```

2) Apuntar `kubectl` al clúster emulado (el k3s de MiniStack) y ver el nodo; después **re-siembra SSM** y **parchea CoreDNS** para que los pods resuelvan `host.docker.internal`. `Prepare` hace los tres pasos (idempotente; ejecutarlo **siempre** tras cada `terraform apply`):

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Prepare -AppHost host.docker.internal
kubectl get nodes
```

> `aws eks update-kubeconfig` genera un contexto con `aws eks get-token`, pero **el k3s de MiniStack no valida ese token**: el helper `Kubeconfig` extrae el kubeconfig **admin** (`/etc/rancher/k3s/k3s.yaml`) del contenedor k3s y lo deja instalado en el contexto `slg-minstack` de tu `~/.kube/config`. Deberías ver 1 nodo `Ready`. A partir de aquí `kubectl`/`helm` operan como si fuera EKS.
>
> El parche de CoreDNS es obligatorio: los pods resuelven `host.docker.internal` vía el `NodeHosts` de CoreDNS (`192.168.65.254`) y sin él fallan con *host not found* al conectar con SQL/RabbitMQ/MiniStack.

3) Construir y subir las imágenes al **ECR emulado** de MiniStack:

```powershell
docker build -f SoftwareLearningGuide.Api/Dockerfile              -t softwarelearningguide/software-guide-api:local  .
docker build -f SoftwareLearningGuide.OutboxProcessor/Dockerfile  -t softwarelearningguide/outbox-processor:local    .
docker build -f SoftwareLearningGuide.Consumer/Dockerfile         -t softwarelearningguide/consumer:local            .

aws --profile ministack --endpoint-url http://localhost:4566 ecr get-login-password `
  | docker login --username AWS --password-stdin localhost:4566

docker tag softwarelearningguide/software-guide-api:local   localhost:4566/softwarelearningguide-api:latest
docker tag softwarelearningguide/outbox-processor:local     localhost:4566/softwarelearningguide-outboxprocessor:latest
docker tag softwarelearningguide/consumer:local             localhost:4566/softwarelearningguide-consumer:latest

docker push localhost:4566/softwarelearningguide-api:latest
docker push localhost:4566/softwarelearningguide-outboxprocessor:latest
docker push localhost:4566/softwarelearningguide-consumer:latest
```

4) Instalar los charts. **`image.repository` debe ser el host del mirror que el k3s preconfigura** (`000000000000.dkr.ecr.us-east-1.amazonaws.com`): ese host ya viene por defecto en cada `values.yaml`, y `values-emulated.yaml` fija la config que apunta a `host.docker.internal`. No uses `host.docker.internal:4566` como `image.repository` (sin mirror, el kubelet hace HTTPS → `ErrImagePull`):

```powershell
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml

helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  -f ./deploy/helm/outbox-processor/values-emulated.yaml

helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  -f ./deploy/helm/consumer/values-emulated.yaml
```

5) Verificar el despliegue y exponer la API:

```powershell
kubectl -n slg get deploy,pods,svc,cm
kubectl -n slg rollout status deploy/software-guide-api --timeout=180s
kubectl -n slg rollout status deploy/outbox-processor --timeout=120s
kubectl -n slg rollout status deploy/consumer --timeout=120s
kubectl -n slg port-forward svc/software-guide-api 5089:80
```

Repite el flujo del Paso 3 y mira los logs de los workers:

```powershell
kubectl -n slg logs -l app.kubernetes.io/instance=outbox-processor -f --tail=50
kubectl -n slg logs -l app.kubernetes.io/instance=consumer -f --tail=50
```

7) Teardown del clúster emulado:

```powershell
helm uninstall software-guide-api outbox-processor consumer -n slg
cd deploy/IaC
terraform destroy -var-file="terraform.minstack.tfvars.example" -auto-approve
```

> `terraform destroy` también borra el clúster k3s que MiniStack embebió.

### Paso 6 — Operar MiniStack con AWS CLI (SSM / SNS / SQS / Cognito)

Con el perfil `ministack` listo:

```powershell
# Ver parámetros SSM
aws --profile ministack --endpoint-url http://localhost:4566 ssm get-parameters-by-path --path "/softwarelearningguide/" --recursive

# Mensajería AWS (MassTransit crea topics/queues al arrancar)
aws --profile ministack --endpoint-url http://localhost:4566 sns list-topics
aws --profile ministack --endpoint-url http://localhost:4566 sqs list-queues

# Cognito: pools / clientes creados por cognito-init.sh
aws --profile ministack --endpoint-url http://localhost:4566 cognito-idp list-user-pools --max-results 50
aws --profile ministack --endpoint-url http://localhost:4566 cognito-idp list-user-pool-clients --user-pool-id <POOL_ID>

# Obtener un token directamente desde MiniStack
$CLIENT_ID = aws --profile ministack --endpoint-url http://localhost:4566 ssm get-parameter `
  --name "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/ClientId" `
  --query Parameter.Value --output text
aws --profile ministack --endpoint-url http://localhost:4566 cognito-idp initiate-auth `
  --client-id $CLIENT_ID --auth-flow USER_PASSWORD_AUTH `
  --auth-parameters USERNAME=admin@test.com,PASSWORD=Test1234!
```

### Paso 7 — Teardown (Emulación Local)

```powershell
docker-compose down -v        # -v borra también los volúmenes (SQL, Redis, RabbitMQ)
```

> Sin `-v` los datos persisten entre reinicios (los parámetros SSM/Cognito de MiniStack no sobreviven).

---

## Terraform: Sin Credenciales, Con Credenciales o contra MiniStack

El estado por defecto es **local** (`deploy/IaC/terraform.tfstate`) y el módulo **no consulta AWS para planear** (las credenciales se saltan por defecto). Puedes formatear, validar y generar planes sin crear nada y **sin credenciales AWS**:

```powershell
cd deploy/IaC
terraform init                 # descarga los providers (solo requiere internet)
terraform fmt -recursive       # formato canónico (HCL)
terraform validate             # valida sintaxis y referencias
terraform plan -var-file="terraform.tfvars.example"   # grafo de recursos sin contactar AWS
```

> El archivo `.terraform.lock.hcl` generado por `terraform init` fija las versiones exactas de los providers; **compételo en git** (`.terraform/` sí está ignorado por `.gitignore`). El `fmt` formatea `.tf` anidados (módulos) y `validate` comprueba la referencia a variables y módulos. Todo esto funciona sin credenciales AWS ni conexión a MiniStack.

Los flags por defecto en `variables.tf` (`aws_skip_credentials_validation=true`, `aws_skip_requesting_account_id=true`) impiden que el provider llame a `STS` durante `plan`/`apply`, así que la planificación funciona sin cadena de credenciales. Si necesitas **creación real en AWS**, añade credenciales válidas:

```powershell
aws configure
terraform apply -var-file="terraform.tfvars.example"   # crea VPC + EKS + ECR (tiene coste)
```

**El punto fuerte de este IaC es que también corre contra MiniStack sin tocar AWS** (Sección 3, Paso 5). Usa el var-file dedicado:

```powershell
terraform init
terraform plan  -var-file="terraform.minstack.tfvars.example"
terraform apply -var-file="terraform.minstack.tfvars.example"
```

> Con `terraform.minstack.tfvars.example` todos los servicios del provider apuntan a `http://localhost:4566` y se desactivan partes que el emulador no soporta (NAT, flow logs, node group, add-ons, Access Entries, OIDC). El clúster EKS emulado levanta un k3s/k3d real embebido en Docker.

### Referencia Rápida

| Escenario | Var-file | Credenciales | Resultado |
|-----------|----------|--------------|-----------|
| `plan` rápido (sin tocar nada) | `terraform.tfvars.example` | Ninguna | Grafo propuesto; no contacta AWS |
| Despliegue real AWS | `terraform.tfvars.example` | `aws configure` | VPC + EKS + ECR reales (coste real) |
| Emulado con MiniStack | `terraform.minstack.*` | `test`/`test` | VPC + ECR + k3s local en `localhost:4566` |

---

## Despliegue Real en AWS (Referencia)

> **Ejecuta esto solo cuando quieras desplegar de verdad. Infraestructura = coste.** Aquí sí se usan **kubectl + Helm** (no en el flujo local).

### Crear la Infraestructura

```powershell
cd deploy/IaC
copy terraform.tfvars.example terraform.tfvars
# edita terraform.tfvars (admin_arns con TUS ARNs, región, tamaños...)

aws configure                    # credenciales de una cuenta real
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply -var-file="terraform.tfvars"   # 10-15 min aprox.
```

Recupera los valores útiles:

```powershell
terraform output kubeconfig_command
terraform output ecr_repository_urls
```

### kubeconfig y Acceso

```powershell
aws eks --region us-east-1 update-kubeconfig --name softwarelearningguide-dev-eks
kubectl get nodes
kubectl get ns
```

Con `eks_admin_arns` relleno, tu usuario ya es admin vía Access Entries (`AmazonEKSClusterAdminPolicy`).

### Construir y Publicar Imágenes en ECR

```powershell
$ACCOUNT = (aws sts get-caller-identity --query Account --output text)
$REGION  = "us-east-1"

docker build -f SoftwareLearningGuide.Api/Dockerfile                  -t softwarelearningguide/software-guide-api:local  .
docker build -f SoftwareLearningGuide.OutboxProcessor/Dockerfile      -t softwarelearningguide/outbox-processor:local    .
docker build -f SoftwareLearningGuide.Consumer/Dockerfile             -t softwarelearningguide/consumer:local            .

docker tag softwarelearningguide/software-guide-api:local   "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-api:latest"
docker tag softwarelearningguide/outbox-processor:local     "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-outboxprocessor:latest"
docker tag softwarelearningguide/consumer:local             "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-consumer:latest"

aws ecr get-login-password --region $REGION | docker login --username AWS `
  --password-stdin "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com"

docker push "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-api:latest"
docker push "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-outboxprocessor:latest"
docker push "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-consumer:latest"
```

### Instalar los Charts con Imágenes de ECR

> En AWS real los servicios derivados (SQL, RabbitMQ, SNS/SQS, Cognito) no están en el clúster: crea RDS SQL Server, AmazonMQ/RabbitMQ, SNS/SQS y el user pool de Cognito, guarda la configuración en SSM bajo `/softwarelearningguide/{env}/{servicio}/` y apunta los charts a esos valores (SSM gana sobre el ConfigMap; con `ServiceUrl` vacío el SDK usa AWS real). Los valores por defecto de los charts (`localhost`) son ilustrativos: ajústalos con `--set`.

```powershell
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg `
  --set image.repository="$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-api" `
  --set image.tag=latest

helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  --set image.repository="$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-outboxprocessor" `
  --set image.tag=latest

helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  --set image.repository="$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-consumer" `
  --set image.tag=latest
```

Para exponer la API fuera del clúster, antepone un Ingress/ALB (o cambia `service.type` a `LoadBalancer` o `NodePort`).

---

## Operación Diaria (AWS Real)

```powershell
# Ver releases instalados
helm ls -n slg

# Actualizar un chart (aplica cambios de values/config)
helm upgrade outbox-processor ./deploy/helm/outbox-processor -n slg

# Ver el diff de lo que se aplicaría sin tocarlo
helm template outbox-processor ./deploy/helm/outbox-processor -n slg

# Rollback
helm history software-guide-api -n slg
helm rollback software-guide-api 1 -n slg

# Escalar / logs / config aplicada
kubectl -n slg scale deploy/software-guide-api --replicas=3
kubectl -n slg logs -l app.kubernetes.io/instance=consumer -f
kubectl -n slg get cm software-guide-api-config -o yaml | Select-Object -First 80
```

---

## Troubleshooting

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| `aws ... endpoint` no responde (Timeout) | MiniStack no está corriendo | `docker-compose ps` → `docker-compose up -d ministack redis` |
| SSM vacío o parcialmente sembrado | Los scripts de seed no se ejecutaron (primera subida con volumen persistente) | `ministack.ps1 -Action Reseed`; o reinicia el contenedor sin volumen; comprueba los logs: `docker compose logs ministack` |
| API responde `400` «Cognito no está configurado» | `UserPoolId`/`ClientId` no están en SSM | Re-ejecuta `cognito-init.sh` dentro del contenedor (Paso 5) y reinicia la API |
| `401` al llamar a la API | Token caducado o no enviado | Obtén uno en `/api/v1/token` (Paso 3) |
| Endpoint responde `404` | Feature flag `FT_*` apagado en Flagsmith | Actívalo en `http://localhost:8000` (en dev la API lee flags de Flagsmith) |
| Outbox/Consumer sin logs | Workers no arrancados | Comprueba las ventanas de consola: `dotnet run ... OutboxProcessor/Consumer` |
| Mensajes duplicados tras reinicio | SQL/RabbitMQ retienen estado entre sesiones | `docker-compose down -v` para reset completo |
| `terraform plan` falla con `InvalidClientTokenId` | Credenciales AWS configuradas no son válidas | `aws configure` para obtener credenciales reales, o usa `terraform.minstack.tfvars.example` |
| Helm no encuentra el chart local | Rutas relativas desde otro directorio | Ejecuta desde la raíz del repo (`./deploy/helm/...`) |
| Pod `ImagePullBackOff` (AWS real) | Imagen no existe en ECR o tag erróneo | Públicala (Sección 5.3) y verifica `image.repository`/`image.tag` |
| `kubectl get nodes` → credentials error | El contexto usa `aws eks get-token`, que el k3s de MiniStack no valida | `ministack.ps1 -Action Kubeconfig` (usa el kubeconfig admin del contenedor k3s) |

---

## Seguridad y Buenas Prácticas

- **Emulación local aislada**: las credenciales `test`/`test` y el perfil `ministack` solo apuntan a MiniStack; no se usan para llamadas a AWS real.
- **Secretos fuera del repo**: en los charts los valores van como valores por defecto de aprendizaje. En producción usa `Secret` + SSM Parameter Store / Secrets Manager e inyéctalos con `extraEnv` o `--set-string config...`.
- **ConfigMap inmutable por checksum**: al cambiar `values.config` el Deployment se re-despliega automáticamente (anotación `checksum/config`).
- **Probes**: la API tiene `readiness`/`liveness` por TCP; los workers (sin listener HTTP) **no** tienen probes para no reiniciarse en bucle.
- **Recursos acotados** (`requests`/`limits`) en los tres charts.
- **ECR**: escaneo en push, política de lifecycle (retener N imágenes, limpiar el resto).
- **EKS**: `authentication_mode = API_AND_CONFIG_MAP` con Access Entries (nada de editar `aws-auth` a mano), control plane logging parametrizado, endpoint público restringible por CIDR, y OIDC provider listo para IRSA.
- **VPC**: subredes privadas para workloads, NAT para egress, Flow Logs opcionales y etiquetado completo (`Name`, `Project`, `Environment`, `ManagedBy`).

---

## Documentación Relacionada

| Tema | Documento |
|------|-----------|
| **Terraform (IaC)** | [`README.Terraform.md`](README.Terraform.md) |
| **Helm (Kubernetes)** | [`README.Helm.md`](README.Helm.md) |
| **MiniStack (emulador local)** | [`README.Ministack.md`](README.Ministack.md) |
| **AWS general (catálogo de servicios)** | [`README.AWS.md`](README.AWS.md) |
| **Configuración de la API** | [`SoftwareLearningGuide.Api/README.Api.md`](../SoftwareLearningGuide.Api/README.Api.md) |
| **Docker Compose local** | [`../../README.md`](../../README.md) |
| **Feature Management** | [`SoftwareLearningGuide.Api/README.FeatureManagement.md`](../SoftwareLearningGuide.Api/README.FeatureManagement.md) |
| **Observabilidad** | [`SoftwareLearningGuide.Api/README.Observability.md`](../SoftwareLearningGuide.Api/README.Observability.md) |

---

**Feliz despliegue!** 🚀
