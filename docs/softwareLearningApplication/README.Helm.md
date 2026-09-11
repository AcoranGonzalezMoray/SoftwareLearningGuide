<h1 align="center">Helm — Gestión de Paquetes para Kubernetes</h1>

<p align="center">
  <em>Guía completa sobre cómo se usa Helm en este proyecto para desplegar las tres aplicaciones (.NET) en un clúster Kubernetes: API, OutboxProcessor y Consumer</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Helm-0F1689?style=for-the-badge&logo=helm&logoColor=white" alt="Helm">
  <img src="https://img.shields.io/badge/Kubernetes-326CE5?style=for-the-badge&logo=kubernetes&logoColor=white" alt="Kubernetes">
  <img src="https://img.shields.io/badge/Chart-00B4D8?style=for-the-badge&logo=helm&logoColor=white" alt="Helm Chart">
  <img src="https://img.shields.io/badge/Container-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Container">
</p>

---

## Tabla de Contenidos

1. [¿Qué es Helm?](#qué-es-helm)
2. [¿Por qué Helm en este proyecto?](#por-qué-helm-en-este-proyecto)
3. [Estructura de un Chart de Helm](#estructura-de-un-chart-de-helm)
4. [Los tres Charts del proyecto](#los-tres-charts-del-proyecto)
    - [software-guide-api](#software-guide-api)
    - [outbox-processor](#outbox-processor)
    - [consumer](#consumer)
5. [Archivo values.yaml — Configuración por defecto](#archivo-valuesyaml--configuración-por-defecto)
6. [Templates — Generación de recursos Kubernetes](#templates--generación-de-recursos-kubernetes)
    - [ConfigMap](#configmap)
    - [Deployment](#deployment)
    - [Service](#service)
    - [Helpers (_helpers.tpl)](#helpers-helperstpl)
    - [NOTES.txt](#notestxt)
7. [Instalación — helm upgrade --install](#instalación--helm-upgrade--install)
8. [Despliegue emulado con MiniStack](#despliegue-emulado-con-ministack)
9. [Despliegue real en AWS EKS](#despliegue-real-en-aws-eks)
10. [Operación diaria](#operación-diaria)
11. [Buenas prácticas y seguridad](#buenas-prácticas-y-seguridad)
12. [Documentación relacionada](#documentación-relacionada)

---

## ¿Qué es Helm?

**Helm** es el **gestor de paquetes de Kubernetes**. Permite definir, instalar y actualizar aplicaciones en un clúster Kubernetes usando **Charts** — paquetes que contienen un conjunto predefinido de recursos Kubernetes.

Un **Chart** es una colección de archivos YAML que describen los recursos de Kubernetes necesarios para ejecutar una aplicación. En lugar de escribir `Deployment`, `Service`, `ConfigMap`, etc. a mano, Helm genera esos recursos a partir de **templates** y un archivo de **valores** (`values.yaml`).

| Característica | Detalle |
|----------------|---------|
| **Lenguaje de templates** | Go Templates |
| **Gestión de releases** | Cada instalación es un "release" con versión |
| **Rollback** | `helm rollback` deshace cambios automáticamente |
| **Values** | Parametrización completa del chart |
| **Repositorios** | Charts públicos en [Artifact Hub](https://artifacthub.io) |
| **Versionado** | Cada chart tiene su propia versión (`Chart.yaml`) |

---

## ¿Por qué Helm en este proyecto?

Este proyecto tiene **tres aplicaciones .NET** que necesitan desplegarse en Kubernetes. Helm permite:

1. **Parametrizar cada aplicación**: cada chart tiene su propio `values.yaml` con configuración específica (imagen, réplicas, recursos, conexión a base de datos).
2. **Despliegue reproducible**: un `helm upgrade --install` despliega exactamente lo mismo en cualquier clúster.
3. **Configuración centralizada**: toda la configuración de la aplicación vive en `values.yaml`, no hay archivos sueltos.
4. **Integración con Terraform**: Terraform crea la infraestructura (VPC, EKS, ECR); Helm despliega las aplicaciones sobre esa infraestructura.
5. **Emulación local**: los charts se pueden probar contra MiniStack sin tocar AWS real.

### Flujo completo del proyecto

```
┌──────────────────────────────────────────────────────────┐
│  1. Terraform (IaC)                                      │
│     Crea la infraestructura: VPC + EKS + ECR             │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────┐
│  2. Construir imágenes Docker                            │
│     dotnet build → Dockerfile → push a ECR               │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────┐
│  3. Helm (Charts)                                        │
│     helm upgrade --install → Despliega las 3 apps        │
│     ├── software-guide-api  → API REST                   │
│     ├── outbox-processor    → Worker Outbox              │
│     └── consumer            → Worker Consumer            │
└──────────────────────────────────────────────────────────┘
```

---

## Estructura de un Chart de Helm

Cada chart sigue esta estructura estándar:

```
deploy/helm/<nombre-del-chart>/
├── Chart.yaml              # Metadatos del chart (nombre, versión, descripción)
├── values.yaml             # Valores por defecto (configuración)
└── templates/
    ├── _helpers.tpl        # Templates reutilizables (nombres, labels)
    ├── configmap.yaml      # Configuración de la app (appsettings.Production.json)
    ├── deployment.yaml     # Despliegue del Pod
    ├── service.yaml        # Servicio de red (ClusterIP / NodePort)
    └── NOTES.txt           # Instrucciones post-instalación
```

---

## Los tres Charts del proyecto

El proyecto tiene **un chart por cada una de las tres aplicaciones .NET**:

### software-guide-api

| Campo | Valor |
|-------|-------|
| **Nombre del chart** | `software-guide-api` |
| **Descripción** | API REST ASP.NET Core del proyecto |
| **Recursos Kubernetes** | Deployment + Service + ConfigMap |
| **Puertos** | 8080 (HTTP) |
| **Probes** | `readinessProbe` y `livenessProbe` (TCP) |
| **Réplicas por defecto** | 2 |

```yaml
# Chart.yaml
apiVersion: v2
name: software-guide-api
description: API REST ASP.NET Core del proyecto Software Learning Guide
type: application
version: 0.1.0
appVersion: "0.1.0"
```

### outbox-processor

| Campo | Valor |
|-------|-------|
| **Nombre del chart** | `outbox-processor` |
| **Descripción** | Worker de procesamiento del Transactional Outbox |
| **Recursos Kubernetes** | Deployment + ConfigMap (solo Deployment, sin Service — no tiene listener HTTP) |
| **Probes** | Ninguna (worker sin listener HTTP) |
| **Réplicas por defecto** | 1 |

```yaml
# Chart.yaml
apiVersion: v2
name: outbox-processor
description: Worker de procesamiento del Transactional Outbox (público RabbitMQ + SNS/SQS)
type: application
version: 0.1.0
appVersion: "0.1.0"
```

### consumer

| Campo | Valor |
|-------|-------|
| **Nombre del chart** | `consumer` |
| **Descripción** | Worker consumidor de Integration Events (RabbitMQ + SNS/SQS) |
| **Recursos Kubernetes** | Deployment + ConfigMap (solo Deployment, sin Service) |
| **Probes** | Ninguna (worker sin listener HTTP) |

```yaml
# Chart.yaml
apiVersion: v2
name: consumer
description: Worker consumidor de Integration Events (RabbitMQ + SNS/SQS)
type: application
version: 0.1.0
appVersion: "0.1.0"
```

### Diferencias entre los tres charts

| Aspecto | API | OutboxProcessor | Consumer |
|---------|-----|-----------------|----------|
| **Service** | ✅ Sí (ClusterIP/NodePort) | ❌ No | ❌ No |
| **Probes** | ✅ readiness + liveness (TCP) | ❌ No | ❌ No |
| **Puerto** | 8080 | N/A | N/A |
| **ConfigMap** | ✅ Sí | ✅ Sí | ✅ Sí |
| **Deployment** | ✅ Sí | ✅ Sí | ✅ Sí |

> **¿Por qué los workers no tienen probes?** OutboxProcessor y Consumer no tienen un listener HTTP, no hay nada contra lo que hacer un `tcpSocket` check. Se escalan basándose en el consumo de RabbitMQ. Si se reiniciaran en bucle por error, no se auto-recuperarían.

---

## Archivo values.yaml — Configuración por defecto

El archivo `values.yaml` es el **corazón del chart**. Define todos los valores por defecto que los templates usan para generar los YAMLs de Kubernetes.

### Estructura de values.yaml (software-guide-api)

```yaml
# ---- Réplicas e imagen ----
replicaCount: 2
image:
  repository: softwarelearningguide/software-guide-api
  pullPolicy: IfNotPresent
  tag: local

# ---- Servicio ----
service:
  type: ClusterIP
  port: 80
  targetPort: 8080

# ---- Recursos ----
resources:
  requests:
    cpu: 100m
    memory: 256Mi
  limits:
    cpu: 500m
    memory: 512Mi

# ---- Variables de entorno ----
env:
  aspnetUrls: "http://+:8080"

# ---- Configuración de la app (→ ConfigMap → appsettings.Production.json) ----
config:
  Logging:
    LogLevel:
      Default: "Information"
      Microsoft.AspNetCore: "Warning"
  ConnectionStrings:
    SoftwareLearningGuide: "Server=localhost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  FeatureManagement:
    FT_ENABLE_ORDER_CONTROLLER: true
    # ... más feature flags ...
  CloudProvidersConfigurations:
    AWS:
      Enabled: false
      Credentials:
        AccessKey: "test"
        AccessSecret: "test"
      SSM:
        Enabled: false
        Path: "/softwarelearningguide/dev/api/"
        Region: "us-east-1"
        ServiceUrl: ""
      Cognito:
        Enabled: false
        UserPoolId: ""
        ClientId: ""
        Region: "us-east-1"
        ServiceUrl: ""
```

### Cómo se inyecta la configuración

El bloque `config` se convierte en un **ConfigMap Kubernetes** que monta `appsettings.Production.json` dentro del container. Esto funciona porque:

1. `ASPNETCORE_ENVIRONMENT=Production` se pasa como variable de entorno.
2. .NET busca automáticamente `appsettings.Production.json`.
3. El ConfigMap monta el archivo en `/app/appsettings.Production.json`.

> **Nota:** `appsettings.json` (base) y `appsettings.Development.json` (desarrollo) se construyen en el proyecto. Solo `appsettings.Production.json` se genera desde `values.yaml`.

### Sobreescritura con --set

Puedes sobreescribir cualquier valor en tiempo de despliegue:

```bash
# Cambiar la conexión a SQL Server
helm upgrade --install api ./deploy/helm/software-guide-api -n slg \
  --set config.ConnectionStrings.SoftwareLearningGuide="Server=10.0.0.50,1433;Database=SoftwareLearningGuide;User Id=sa;Password=Production!2024;"

# Cambiar el número de réplicas
helm upgrade --install api ./deploy/helm/software-guide-api -n slg \
  --set replicaCount=5

# Activar AWS (SSM + Cognito) contra MiniStack
helm upgrade --install api ./deploy/helm/software-guide-api -n slg \
  --set config.CloudProvidersConfigurations.AWS.Enabled=true \
  --set config.CloudProvidersConfigurations.AWS.SSM.Enabled=true \
  --set config.CloudProvidersConfigurations.AWS.SSM.ServiceUrl="http://host.docker.internal:4566"
```

---

## Templates — Generación de recursos Kubernetes

### ConfigMap

Genera un **ConfigMap** con el archivo `appsettings.Production.json`:

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

- `{{ include "slg.fullname" . }}`: genera el nombre del recurso (ej: `software-guide-api-config`).
- `{{- toPrettyJson .Values.config | nindent 4 }}`: convierte el bloque `config` de `values.yaml` a JSON formateado con indentación de 4 espacios.

El resultado es un ConfigMap con toda la configuración de producción de la app.

### Deployment

El `Deployment` define el **Pod** que ejecuta la aplicación:

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
  selector:
    matchLabels:
      {{- include "slg.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "slg.selectorLabels" . | nindent 8 }}
      annotations:
        # Fuerza rollout cuando cambia la ConfigMap
        checksum/config: {{ include (print $.Template.BasePath "/configmap.yaml") . | sha256sum }}
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

**Elementos clave:**

| Elemento | Descripción |
|----------|-------------|
| **RollingUpdate** | `maxSurge: 25%`, `maxUnavailable: 0` → despliegue sin downtime |
| **checksum/config** | Anotación SHA256 del ConfigMap. Al cambiar `values.config`, el hash cambia y Kubernetes fuerza un nuevo rollout |
| **volumeMounts** | Monta el ConfigMap como archivo en `/app/appsettings.Production.json` |
| **readinessProbe** | `tcpSocket` en el puerto HTTP → Kubernetes solo envía tráfico cuando el app está listo |
| **livenessProbe** | `tcpSocket` en el puerto HTTP → Kubernetes reinicia el pod si no responde |

> **¿Por qué `maxUnavailable: 0`?** Garantiza que siempre haya al menos un pod disponible durante el despliegue. El nuevo pod se arranca antes de que el viejo se detenga.

### Service

El `Service` expone la API dentro del clúster:

```yaml
# templates/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: {{ include "slg.fullname" . }}
spec:
  type: {{ .Values.service.type }}
  ports:
    - port: {{ .Values.service.port }}
      targetPort: {{ .Values.service.targetPort }}
      protocol: TCP
  selector:
    {{- include "slg.selectorLabels" . | nindent 4 }}
```

Por defecto es `ClusterIP` (acceso interno del clúster). Para acceder desde fuera se usa `kubectl port-forward` o se cambia a `NodePort` / `LoadBalancer`.

### Helpers (_helpers.tpl)

Contiene **templates reutilizables** para nombres y labels:

```yaml
# templates/_helpers.tpl
{{- define "slg.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Chart.Name (include "slg.suffix" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{- define "slg.labels" -}}
app.kubernetes.io/name: {{ include "slg.fullname" . }}
app.kubernetes.io/instance: {{ .Chart.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}
```

Los labels `app.kubernetes.io/*` son los **standard labels de Kubernetes** que permiten seleccionar recursos con `kubectl`:

```bash
kubectl get pods -l app.kubernetes.io/instance=software-guide-api
```

### NOTES.txt

Instrucciones que se muestran después de instalar el chart:

```text
================================================================================
Software Guide API has been installed!
================================================================================

To verify the deployment:
  kubectl -n slg get pods -l app.kubernetes.io/instance=software-guide-api
  kubectl -n slg get svc software-guide-api

To port-forward to access the API locally:
  kubectl -n slg port-forward svc/software-guide-api 5089:80

Then open: http://localhost:5089/swagger
================================================================================
```

---

## Instalación — helm upgrade --install

### Comando base

El comando estándar para instalar o actualizar un chart es:

```bash
helm upgrade --install <release-name> <chart-path> -n <namespace> [flags]
```

| Parámetro | Descripción |
|-----------|-------------|
| `upgrade --install` | Si el release existe, lo actualiza; si no, lo instala |
| `<release-name>` | Nombre lógico del release (ej: `software-guide-api`) |
| `<chart-path>` | Ruta al chart (`./deploy/helm/software-guide-api`) |
| `-n slg` | Namespace de Kubernetes donde se despliega |
| `--create-namespace` | Crea el namespace si no existe |

### Crear el namespace

```bash
kubectl create namespace slg
```

### Ejemplo de instalación completa (emulado con MiniStack)

> **Importante (ErrImagePull):** `image.repository` debe ser el host del **mirror**
> que el k3s de MiniStack deja configurado (`000000000000.dkr.ecr.us-east-1.amazonaws.com`).
> `host.docker.internal:4566` NO sirve para `image.repository`: el kubelet no tiene mirror
> para ese host e intenta ECR por HTTPS → `ErrImagePull`. Los charts ya traen el valor
> correcto por defecto (`values.yaml`) y `values-emulated.yaml` fija la configuración
> que apunta a `host.docker.internal` (SQL/RabbitMQ/MiniStack).

```bash
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml
```

Para los tres servicios:

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

## Despliegue emulado con MiniStack

> **Este es el modo que practica todo el flujo sin tocar AWS real.**

### Arquitectura

```
┌──────────────────────────────────────────────────────────┐
│  MINISTACK (localhost:4566)                               │
│  ├── SSM Parameter Store                                 │
│  ├── SNS (tópicos)                                       │
│  ├── SQS (colas)                                         │
│  └── Cognito (User Pool)                                 │
└──────────────────────────┬───────────────────────────────┘
                           │ localhost:4566
                           ▼
┌──────────────────────────────────────────────────────────┐
│  K3s (MiniStack embebido)                                 │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Namespace: slg                                     │  │
│  │  ├── software-guide-api (Deployment + Service)    │  │
│  │  ├── outbox-processor (Deployment)                 │  │
│  │  └── consumer (Deployment)                         │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────┬───────────────────────────────┘
                           │ host.docker.internal
                           ▼
┌──────────────────────────────────────────────────────────┐
│  HOST (tu máquina)                                        │
│  ├── SQL Server (puerto 1433)                             │
│  ├── RabbitMQ (puerto 5672)                               │
│  └── MiniStack (puerto 4566)                              │
└──────────────────────────────────────────────────────────┘
```

### Paso a paso completo

```bash
# 1. Levantar MiniStack y la infraestructura de apoyo
docker-compose up -d

# 2. Inicializar Terraform contra MiniStack
cd deploy/IaC
terraform init
terraform apply -var-file="terraform.minstack.tfvars.example"

# 3. Post-apply (SIEMPRE tras cada apply/recreate): kubeconfig para kubectl,
#    re-siembra de SSM apuntando al host y parche del CoreDNS para que los pods
#    resuelvan host.docker.internal
cd ..
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Prepare -AppHost host.docker.internal
kubectl get nodes   # → Debería mostrar 1 nodo Ready

# 4. Construir y push de las imágenes al ECR emulado
docker build -f SoftwareLearningGuide.Api/Dockerfile -t softwarelearningguide/software-guide-api:local .
docker build -f SoftwareLearningGuide.OutboxProcessor/Dockerfile -t softwarelearningguide/outbox-processor:local .
docker build -f SoftwareLearningGuide.Consumer/Dockerfile -t softwarelearningguide/consumer:local .

docker tag softwarelearningguide/software-guide-api:local localhost:4566/softwarelearningguide-api:latest
docker tag softwarelearningguide/outbox-processor:local localhost:4566/softwarelearningguide-outboxprocessor:latest
docker tag softwarelearningguide/consumer:local localhost:4566/softwarelearningguide-consumer:latest

docker push localhost:4566/softwarelearningguide-api:latest
docker push localhost:4566/softwarelearningguide-outboxprocessor:latest
docker push localhost:4566/softwarelearningguide-consumer:latest

# 5. Instalar los charts (image.repository y config ya apuntan al emulador:
#    no hace falta --set image.repository)
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml

helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  -f ./deploy/helm/outbox-processor/values-emulated.yaml

helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  -f ./deploy/helm/consumer/values-emulated.yaml

# 6. Verificar
kubectl -n slg get deploy,pods,svc,cm

# 7. Probar la API
kubectl -n slg port-forward svc/software-guide-api 5089:80
curl -X POST http://localhost:5089/api/v1/token -d "username=admin@test.com&password=Test1234!"
```

### Teardown

```bash
# Desinstalar los charts
helm uninstall software-guide-api outbox-processor consumer -n slg

# Destruir la infraestructura Terraform (incluido el clúster k3s)
cd deploy/IaC
terraform destroy -var-file="terraform.minstack.tfvars.example" -auto-approve
```

---

## Despliegue real en AWS EKS

> **Ejecuta esto solo cuando quieras desplegar de verdad. Infraestructura = coste.**

### Flujo

1. **Terraform** crea VPC + EKS + ECR en AWS real.
2. **Docker** construye las imágenes y las `docker push` a ECR.
3. **Helm** instala los charts apuntando a las imágenes de ECR.
4. **Los servicios AWS** (RDS, SNS/SQS, Cognito) se gestionan por separado o se configuran desde SSM.

```bash
# 1. Crear infraestructura
cp terraform.tfvars.example terraform.tfvars
# Editar terraform.tfvars con tus ARNs, región, etc.

terraform init
terraform apply -var-file="terraform.tfvars"

# 2. Obtener el kubeconfig
aws eks --region us-east-1 update-kubeconfig --name softwarelearningguide-dev-eks

# 3. Construir y push de imágenes a ECR
$ACCOUNT = (aws sts get-caller-identity --query Account --output text)
$REGION = "us-east-1"

docker build -f SoftwareLearningGuide.Api/Dockerfile -t softwarelearningguide/software-guide-api:local .
# ... push a ECR ...

# 4. Instalar los charts con imágenes de ECR
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

---

## Operación diaria

### Comandos esenciales de Helm

```bash
# Ver los releases instalados
helm ls -n slg

# Ver el estado de un release
helm status software-guide-api -n slg

# Ver el diff (qué se cambiaría sin aplicar)
helm diff upgrade software-guide-api ./deploy/helm/software-guide-api -n slg

# Actualizar (cambia de values o se actualiza la imagen)
helm upgrade software-guide-api ./deploy/helm/software-guide-api -n slg

# Ver el template generado (sin aplicar)
helm template software-guide-api ./deploy/helm/software-guide-api -n slg

# Rollback a la versión anterior
helm history software-guide-api -n slg
helm rollback software-guide-api 1 -n slg

# Escalar
kubectl -n slg scale deploy/software-guide-api --replicas=5

# Ver logs
kubectl -n slg logs -l app.kubernetes.io/instance=consumer -f
kubectl -n slg logs deploy/outbox-processor -f

# Ver ConfigMap aplicado
kubectl -n slg get cm software-guide-api-config -o yaml | Select-Object -First 80
```

### Verificación post-instalación

```bash
# Ver pods, deployments, services y configmaps
kubectl -n slg get deploy,pods,svc,cm

# Verificar que todos los pods están Running
kubectl -n slg rollout status deploy/software-guide-api --timeout=180s
kubectl -n slg rollout status deploy/outbox-processor --timeout=120s
kubectl -n slg rollout status deploy/consumer --timeout=120s

# Exponer la API para pruebas locales
kubectl -n slg port-forward svc/software-guide-api 5089:80
```

---

## Buenas prácticas y seguridad

### Buenas prácticas aplicadas

- **ConfigMap inmutable por checksum**: la anotación `checksum/config` en el Deployment garantiza que cualquier cambio en `values.config` dispare un rollout automático.
- **RollingUpdate sin downtime**: `maxSurge: 25%` y `maxUnavailable: 0` aseguran actualizaciones progresivas sin interrumpir el servicio.
- **Probes para la API**: `readinessProbe` y `livenessProbe` por TCP garantizan que el tráfico solo llega a pods sanos.
- **Resources acotados**: cada pod tiene `requests` y `limits` de CPU/memoria para evitar el abuso de recursos.
- **Namespace aislado**: todo vive en el namespace `slg`, facilitando la gestión y el teardown (`helm uninstall -n slg`).
- **Imágenes con tag**: `image.tag=latest` en dev; versiones específicas en producción.
- **Secretos fuera del repo**: en producción, los secrets van en Kubernetes `Secret` o en SSM Parameter Store / Secrets Manager, no en `values.yaml`.

### Seguridad

- **`ASPNETCORE_ENVIRONMENT=Production`**: los charts despliegan siempre en modo producción para que .NET cargue `appsettings.Production.json`.
- **Sin credentials en los charts**: los valores de acceso se inyectan desde SSM o Secrets Manager.
- **Network Policies** (no implementado aún): se recomienda añadir políticas de red para restrictir el tráfico entre pods.
- **RBAC**: el clúster EKS usa Access Entries (`authentication_mode = API_AND_CONFIG_MAP`) en lugar de editar `aws-auth` manualmente.

---

## Documentación relacionada

| Tema | Documento |
|------|-----------|
| **Terraform (IaC)** | [`README.Terraform.md`](README.Terraform.md) |
| **MiniStack (emulador local)** | [`README.Ministack.md`](README.Ministack.md) |
| **AWS general (catálogo de servicios)** | [`README.AWS.md`](README.AWS.md) |
| **Despliegue completo (IaC + Helm)** | [`README.Deploy.md`](README.Deploy.md) |
| **Configuración de la API** | [`SoftwareLearningGuide.Api/README.Api.md`](../SoftwareLearningGuide.Api/README.Api.md) |
| **Docker Compose local** | [`README.md`](../../../README.md) |
| **Feature Management** | [`SoftwareLearningGuide.Api/README.FeatureManagement.md`](../SoftwareLearningGuide.Api/README.FeatureManagement.md) |
| **Observabilidad** | [`SoftwareLearningGuide.Api/README.Observability.md`](../SoftwareLearningGuide.Api/README.Observability.md) |

---

**Feliz despliegue con Helm!** 🚀
