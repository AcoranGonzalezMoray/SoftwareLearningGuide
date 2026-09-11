<h1 align="center">Terraform — Infraestructura como Código (IaC)</h1>

<p align="center">
  <em>Guía completa sobre cómo se usa Terraform en este proyecto para definir la infraestructura en AWS (VPC, ECR, EKS) y cómo se puede ejecutar contra MiniStack para emulación local sin tocar AWS real</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Terraform-7B42BC?style=for-the-badge&logo=terraform&logoColor=white" alt="Terraform">
  <img src="https://img.shields.io/badge/IaC-Infrastructure%20as%20Code-FF6B6B?style=for-the-badge" alt="IaC">
  <img src="https://img.shields.io/badge/AWS-FF9900?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS">
  <img src="https://img.shields.io/badge/MiniStack-45B7D1?style=for-the-badge&logo=docker&logoColor=white" alt="MiniStack">
</p>

---

## Tabla de Contenidos

1. [¿Qué es Terraform?](#qué-es-terraform)
2. [¿Por qué Terraform en este proyecto?](#por-qué-terraform-en-este-proyecto)
3. [Estructura de archivos Terraform](#estructura-de-archivos-terraform)
4. [Provider AWS](#provider-aws)
5. [Módulos](#módulos)
    - [VPC — Red Virtual](#vpc--red-virtual)
    - [ECR — Repositorio de Imágenes](#ecr--repositorio-de-imágenes)
    - [EKS — Clúster Kubernetes](#eks--clúster-kubernetes)
6. [Variables y archivos .tfvars](#variables-y-archivos-tfvars)
7. [Modo emulación local contra MiniStack](#modo-emulación-local-contra-ministack)
8. [Modo AWS real (referencia)](#modo-aws-real-referencia)
9. [Outputs — valores de salida](#outputs--valores-de-salida)
10. [Flujo de trabajo: comandos esenciales](#flujo-de-trabajo-comandos-esenciales)
11. [Buenas prácticas y seguridad](#buenas-prácticas-y-seguridad)
12. [Documentación relacionada](#documentación-relacionada)

---

## ¿Qué es Terraform?

**Terraform** es una herramienta de **Infraestructura como Código (IaC)** de HashiCorp que permite definir, aprovisionar y gestionar infraestructura en la nube mediante archivos de configuración declarativos escritos en **HCL (HashiCorp Configuration Language)**.

En lugar de crear recursos manualmente a través de la consola de AWS o la CLI, Terraform permite:

- **Definir** la infraestructura como código (versionado, revisable, reusable)
- **Planificar** los cambios antes de aplicarlos (ver qué se va a crear/modificar/borrar)
- **Aplicar** los cambios de forma automática y consistente
- **Destruir** toda la infraestructura limpiamente cuando ya no se necesita
- **Mantener el estado** de la infraestructura en un archivo de estado (`terraform.tfstate`)

| Característica | Detalle |
|----------------|---------|
| **Lenguaje** | HCL (HashiCorp Configuration Language) |
| **Modelo** | Declarativo (describes *qué* quieres, Terraform calcula *cómo* hacerlo) |
| **Estado** | Archivo local (`terraform.tfstate`) o remoto (S3 + DynamoDB) |
| **Proveedores** | AWS, Azure, GCP, Kubernetes, y +300 proveedores más |
| **Versionado** | Cada módulo/recurso tiene su versión |
| **Planificación** | `terraform plan` muestra el grafo de cambios antes de ejecutar |

---

## ¿Por qué Terraform en este proyecto?

Este proyecto educativo usa Terraform con **dos objetivos principales**:

1. **Aprender IaC**: Documentar cómo se despliega la infraestructura de un proyecto .NET en AWS usando código, no clics manuales.
2. **Emulación local con MiniStack**: Terraform puede ejecutarse **contra MiniStack** (el emulador de AWS en `localhost:4566`), lo que permite hacer `terraform plan` y `terraform apply` **sin necesidad de una cuenta AWS ni costes**.

> **Diseño clave:** el código Terraform está preparado para funcionar en **dos modos** — emulación local (contra MiniStack) y AWS real — sin cambiar una sola línea de código HCL. Solo cambia el archivo de variables (`.tfvars`).

### Ventajas de este enfoque

| Aspecto | Beneficio |
|---------|-----------|
| **Sin coste** | Puedes practicar `terraform plan` y `terraform apply` sin pagar nada |
| **Seguridad** | No tocas AWS real a menos que lo decidas explícitamente |
| **Reproducibilidad** | Cualquier desarrollador puede levantar la misma infraestructura |
| **Versionado** | El código HCL vive en git, con historial de cambios |
| **Aprendizaje** | Entiendes cómo se despliega EKS, ECR y VPC en AWS real |

---

## Estructura de archivos Terraform

La carpeta `deploy/IaC/` contiene todo el código Terraform:

```
deploy/IaC/
├── main.tf                          # Providers + orquestación de módulos
├── variables.tf                     # Todas las variables parametrizadas
├── outputs.tf                       # Valores de salida útiles después del apply
├── versions.tf                      # Versiones de Terraform/providers + backend
├── terraform.tfvars.example         # Valores de ejemplo (copia a terraform.tfvars)
├── terraform.minstack.tfvars.example # Valores para emulación contra MiniStack
├── terraform.tfstate                # Estado local (generado por terraform apply)
├── terraform.tfstate.backup         # Backup del estado anterior
├── .terraform.lock.hcl              # Bloqueo de versiones de providers
└── modules/
    ├── vpc/
    │   ├── main.tf                  # VPC, subredes, NAT, Internet Gateway, Flow Logs
    │   ├── variables.tf             # Variables específicas del módulo VPC
    │   ├── outputs.tf               # vpc_id, subnet_ids
    │   └── versions.tf              # Versiones del provider dentro del módulo
    ├── ecr/
    │   ├── main.tf                  # Repositorios ECR + lifecycle policy + escaneo
    │   ├── variables.tf             # Variables específicas del módulo ECR
    │   ├── outputs.tf               # repository_names, repository_urls, docker_tags
    │   └── versions.tf
    └── eks/
        ├── main.tf                  # Clúster EKS, managed node group, add-ons, OIDC
        ├── variables.tf             # Variables específicas del módulo EKS
        ├── outputs.tf               # cluster_name, endpoint, kubeconfig_command, OIDC
        └── versions.tf
```

### Archivo principal: `main.tf`

El `main.tf` es el punto de entrada. Define:

1. **El provider AWS** con configuración condicional (¿apuntar a MiniStack o a AWS real).
2. **Tres módulos** que se comunican entre sí:
   - `vpc` → proporciona `vpc_id` y `subnet_ids` al módulo `eks`
   - `ecr` → crea los repositorios de imágenes
   - `eks` → crea el clúster EKS usando la VPC del módulo `vpc`

```hcl
# main.tf — simplificado
provider "aws" {
  region                      = var.region
  access_key                  = var.aws_access_key
  secret_key                  = var.aws_secret_key
  skip_credentials_validation = var.aws_skip_credentials_validation
  skip_requesting_account_id  = var.aws_skip_requesting_account_id

  # Si aws_minstack_endpoint está relleno, TODOS los servicios apuntan a MiniStack
  dynamic "endpoints" {
    for_each = var.aws_minstack_endpoint != "" ? [1] : []
    content {
      ec2   = var.aws_minstack_endpoint
      eks   = var.aws_minstack_endpoint
      ecr   = var.aws_minstack_endpoint
      ...
    }
  }
}

module "vpc" { source = "./modules/vpc" ... }
module "ecr"  { source = "./modules/ecr"  ... }
module "eks"  { source = "./modules/eks"  ... }
```

---

## Provider AWS

El **provider AWS** es el bloque fundamental que conecta Terraform con los servicios de AWS. Se configura en `main.tf`:

```hcl
provider "aws" {
  region                      = var.region
  access_key                  = var.aws_access_key
  secret_key                  = var.aws_secret_key
  skip_credentials_validation = var.aws_skip_credentials_validation
  skip_requesting_account_id  = var.aws_skip_requesting_account_id
  skip_metadata_api_check     = true

  # Bloque dinámico: solo se inyecta si aws_minstack_endpoint tiene valor
  dynamic "endpoints" {
    for_each = var.aws_minstack_endpoint != "" ? [1] : []
    content {
      ec2        = var.aws_minstack_endpoint
      sts        = var.aws_minstack_endpoint
      iam        = var.aws_minstack_endpoint
      ecr        = var.aws_minstack_endpoint
      eks        = var.aws_minstack_endpoint
      s3         = var.aws_minstack_endpoint
      logs       = var.aws_minstack_endpoint
      cloudwatch = var.aws_minstack_endpoint
      cognitoidp = var.aws_minstack_endpoint
    }
  }
}
```

### Cómo funciona la emulación con MiniStack

Cuando `aws_minstack_endpoint = "http://localhost:4566"`, el bloque `endpoints` se inyecta en el provider y **todos los servicios AWS se redirigen a MiniStack**:

| Servicio | Comportamiento normal | Comportamiento con MiniStack |
|----------|----------------------|------------------------------|
| `ec2` | Llama a la API de EC2 real | Llama a `http://localhost:4566` |
| `eks` | Crea un clúster EKS real | Crea un clúster **k3s embebido en Docker** |
| `ecr` | Crea repositorios en ECR real | Crea repositorios en el MiniStack |
| `sts` | Valida credenciales con STS | Pasa de largo (gracias a `skip_credentials_validation`) |
| `iam` | Crea roles/policies reales | Crea roles/policies en MiniStack |

> **Dos flags clave:** `aws_skip_credentials_validation = true` y `aws_skip_requesting_account_id = true` evitan que el provider llame a STS durante `plan`/`apply`. Esto permite ejecutar `terraform plan` **sin credenciales AWS válidas**, lo cual es esencial para la emulación local.

---

## Módulos

Terraform organiza la infraestructura en **módulos reutilizables** dentro de `deploy/IaC/modules/`. Cada módulo tiene su propio `main.tf`, `variables.tf`, `outputs.tf` y `versions.tf`.

### VPC — Red Virtual

> **Módulo:** `modules/vpc`

Crea la **red aislada** donde todo se despliega: VPC, subredes públicas/privadas, NAT Gateway, Internet Gateway y VPC Flow Logs.

| Recurso | Descripción |
|---------|-------------|
| **VPC** | Red principal con CIDR `10.0.0.0/16` |
| **Subredes públicas** | 3 AZs (`us-east-1a`, `us-east-1b`, `us-east-1c`) — donde vive el load balancer |
| **Subredes privadas** | Donde viven los nodos de EKS |
| **NAT Gateway** | Permite salida a Internet desde subredes privadas (coste fijo ~`$32.85/mes`) |
| **Internet Gateway** | Conexión de la VPC a Internet |
| **VPC Flow Logs** | Auditoría del tráfico de red (opcional, se puede desactivar en local) |

```bash
# Crear una VPC con subredes en 3 AZs
module "vpc" {
  source = "./modules/vpc"
  name_prefix        = "softwarelearningguide-dev"
  cidr               = "10.0.0.0/16"
  azs                = ["us-east-1a", "us-east-1b", "us-east-1c"]
  enable_nat_gateway = true
  enable_flow_logs   = true
}
```

### ECR — Repositorio de Imágenes

> **Módulo:** `modules/ecr`

Crea **tres repositorios ECR** (uno por servicio del proyecto) con política de lifecycle y escaneo de vulnerabilidades:

| Repositorio | Imagen |
|-------------|--------|
| `softwarelearningguide-api` | API REST ASP.NET Core |
| `softwarelearningguide-outboxprocessor` | Worker del Transactional Outbox |
| `softwarelearningguide-consumer` | Worker consumidor de Integration Events |

```bash
# Crear los repositorios ECR
module "ecr" {
  source = "./modules/ecr"
  repositories = [
    "softwarelearningguide-api",
    "softwarelearningguide-outboxprocessor",
    "softwarelearningguide-consumer",
  ]
  scan_on_push         = true       # Escanea en cada push
  lifecycle_max_images = 10          # Retiene solo las últimas 10 imágenes
  image_tag_mutability = "MUTABLE"   # Permite sobreescribir tags (dev)
}
```

### EKS — Clúster Kubernetes

> **Módulo:** `modules/eks`

Crea el **clúster EKS** donde se instalan los charts Helm. Gestiona el plano de control, el managed node group, los add-ons, las Access Entries (para `kubectl`) y el OIDC provider (para IRSA).

| Recurso | Descripción |
|---------|-------------|
| **Clúster EKS** | Plano de control gestionado por AWS (v1.31) |
| **Managed Node Group** | Nodos EC2 `t3.medium` donde corren los pods |
| **Add-ons** | `vpc-cni`, `kube-proxy`, `coredns` |
| **Access Entries** | Permisos de IAM para `kubectl` (nada de editar `aws-auth`) |
| **OIDC Provider** | Habilita IRSA (IAM Roles for Service Accounts) |
| **Logging** | Log del plano de control (api, audit, authenticator) |

```bash
# Crear el clúster EKS
module "eks" {
  source = "./modules/eks"
  cluster_name                 = "softwarelearningguide-dev-eks"
  cluster_version              = "1.31"
  vpc_id                       = module.vpc.vpc_id
  subnet_ids                   = module.vpc.private_subnet_ids
  node_desired_size            = 2
  node_instance_types          = ["t3.medium"]
  enable_cluster_logging       = ["api", "audit", "authenticator"]
  admin_arns                   = []
  create_access_entries        = true
  create_oidc_provider         = true
}
```

---

## Variables y archivos .tfvars

Todas las configuraciones se parametrizan mediante **variables** en `variables.tf`. Los valores se pasan con archivos `.tfvars`.

### `variables.tf` — Variable globales

```hcl
variable "project_name" {
  description = "Nombre del proyecto; se usa como prefijo de los recursos creados."
  type        = string
  default     = "softwarelearningguide"
}

variable "environment" {
  description = "Entorno de despliegue (dev, staging, prod)."
  type        = string
  default     = "dev"
}

variable "region" {
  description = "Región AWS por defecto."
  type        = string
  default     = "us-east-1"
}

variable "aws_minstack_endpoint" {
  description = "Si se rellena (http://localhost:4566), TODOS los servicios apuntan a MiniStack."
  type        = string
  default     = ""
}

variable "aws_skip_credentials_validation" {
  description = "No llama a STS. Necesario para MiniStack y para plan sin credenciales."
  type        = bool
  default     = true
}
```

Los **toggles de EKS** (`eks_create_managed_node_group`, `eks_create_addons`, `eks_create_oidc_provider`, etc.) son especialmente importantes porque permiten desactivar partes de EKS que **MiniStack no soporta**:

| Variable | AWS real | MiniStack |
|----------|----------|-----------|
| `eks_create_managed_node_group` | `true` (crea nodos EC2) | `false` (k3s embebido ya tiene su nodo) |
| `eks_create_addons` | `true` (vpc-cni, coredns) | `false` (k3s incluye sus propios add-ons) |
| `eks_create_access_entries` | `true` | `false` (k3s usa su propio acceso) |
| `eks_create_oidc_provider` | `true` (IRSA) | `false` (no se usa IRSA en local) |
| `enable_nat_gateway` | `true` | `false` (no se necesita en local) |
| `enable_vpc_flow_logs` | `true` | `false` (no se usa en local) |

### `terraform.tfvars.example` — Plantilla para AWS real

```hcl
project_name = "softwarelearningguide"
environment  = "dev"
region       = "us-east-1"

# Para AWS real: dejar aws_access_key y aws_secret_key vacíos
# (el provider las resolve del entorno / shared credentials)
aws_skip_credentials_validation = true
aws_skip_requesting_account_id  = true

# Para VPC real
availability_zones = ["us-east-1a", "us-east-1b", "us-east-1c"]
vpc_cidr           = "10.0.0.0/16"
enable_nat_gateway = true

# Para EKS real
eks_cluster_version              = "1.31"
eks_node_desired_size            = 2
eks_create_managed_node_group    = true
eks_create_addons                = true
eks_create_access_entries        = true
eks_create_oidc_provider         = true
```

### `terraform.minstack.tfvars.example` — Plantilla para MiniStack

```hcl
# Rellena el endpoint para que TODO apunte a MiniStack
aws_access_key         = "test"
aws_secret_key         = "test"
aws_minstack_endpoint  = "http://localhost:4566"

# Desactiva todo lo que MiniStack no soporta
enable_nat_gateway = false
single_nat_gateway = false
enable_vpc_flow_logs = false

eks_enable_access_config      = false
eks_create_managed_node_group = false
eks_create_addons             = false
eks_create_access_entries     = false
eks_create_oidc_provider      = false
```

> **Uso:**
> ```bash
> cp terraform.minstack.tfvars.example terraform.tfvars
> terraform init
> terraform plan -var-file="terraform.tfvars"
> terraform apply -var-file="terraform.tfvars"
> ```

---

## Modo emulación local contra MiniStack

> **Este es el modo que usa el proyecto para practicar sin tocar AWS real.**

### Cómo funciona

Cuando usas `terraform.minstack.tfvars.example`, Terraform se conecta a MiniStack (`http://localhost:4566`) y:

1. **VPC**: MiniStack emula EC2/VPC (a partir de la versión 1.4.9). Crea la VPC y las subredes.
2. **ECR**: Crea repositorios de imágenes en el MiniStack. Puedes `docker push` contra `localhost:4566`.
3. **EKS**: MiniStack crea un **clúster k3s real embebido en Docker**. El clúster k3s tiene su propio nodo y sus propios componentes de red.

```
┌──────────────────────────────────────────────────────────┐
│  MINISTACK (Docker contenedor)                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Puerto 4566                                       │  │
│  │  ├── /ecr   → Repositorios ECR emulados           │  │
│  │  ├── /eks   → Clúster k3s embebido                │  │
│  │  ├── /ec2   → VPC, subredes, NAT emulados         │  │
│  │  └── /iam   → Roles/policies emulados             │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
         ↕ localhost:4566 (aws_minstack_endpoint)
┌──────────────────────────────────────────────────────────┐
│  TERRAFORM (en tu máquina)                               │
│  terraform plan -var-file="terraform.minstack.tfvars"    │
│  terraform apply -var-file="terraform.minstack.tfvars"   │
└──────────────────────────────────────────────────────────┘
```

### Paso a paso — Flujo completo emulado

```bash
# 1. Levantar MiniStack y Redis
docker-compose up -d ministack redis

# 2. Inicializar Terraform (descarga providers)
cd deploy/IaC
terraform init

# 3. Verificar sintaxis y formato
terraform fmt -recursive
terraform validate

# 4. Planificar contra MiniStack (sin tocar AWS real)
terraform plan -var-file="terraform.minstack.tfvars.example"

# 5. Aplicar (crea VPC, ECR y EKS emulados en MiniStack)
terraform apply -var-file="terraform.minstack.tfvars.example"

# 6. Ver los recursos creados
terraform output ecr_repository_urls
terraform output eks_cluster_name

# 7. Teardown (destruye todo, incluido el clúster k3s de MiniStack)
terraform destroy -var-file="terraform.minstack.tfvars.example" -auto-approve
```

### Verificación de recursos emulados

```powershell
# Ver repositorios ECR creados
aws --endpoint-url http://localhost:4566 ecr describe-repositories

# Ver el clúster EKS emulado (k3s)
aws --endpoint-url http://localhost:4566 eks describe-cluster --name softwarelearningguide-dev-eks

# Ver la VPC creada
aws --endpoint-url http://localhost:4566 ec2 describe-vpcs
```

---

## Modo AWS real (referencia)

> **Ejecuta esto solo cuando quieras desplegar de verdad. Infraestructura = coste.**

### Requisitos previos

| Herramienta | Instalación (Windows) |
|-------------|-----------------------|
| AWS CLI | `winget install Amazon.AWSCLI` |
| Terraform | `winget install Hashicorp.Terraform` |
| Credenciales AWS | `aws configure` |

### Paso a paso — AWS real

```bash
# 1. Crear el archivo de variables
cp terraform.tfvars.example terraform.tfvars
# Editar terraform.tfvars: rellenar admin_arns, región, tamaños...

# 2. Inicializar
terraform init

# 3. Planificar (sin crear nada aún)
terraform plan -var-file="terraform.tfvars"

# 4. Aplicar (crea VPC + EKS + ECR — 10-15 min aprox.)
terraform apply -var-file="terraform.tfvars"

# 5. Obtener valores útiles
terraform output kubeconfig_command
terraform output ecr_repository_urls
```

### Referencia de escenarios

| Escenario | Var-file | Credenciales | Resultado |
|-----------|----------|--------------|-----------|
| `plan` rápido (sin tocar nada) | `terraform.tfvars.example` | Ninguna | Grafo propuesto; no contacta AWS |
| Despliegue real AWS | `terraform.tfvars` | `aws configure` | VPC + EKS + ECR reales (coste real) |
| Emulado con MiniStack | `terraform.minstack.tfvars` | `test`/`test` | VPC + ECR + k3s local en `localhost:4566` |

---

## Outputs — valores de salida

El archivo `outputs.tf` define los **valores que Terraform expone** después del `apply`. Son los datos que necesitas para los siguientes pasos:

| Output | Descripción | Uso |
|--------|-------------|-----|
| `vpc_id` | ID de la VPC creada | Referencia para otros recursos |
| `public_subnet_ids` | Subredes públicas | Balanceo de carga |
| `private_subnet_ids` | Subredes privadas | Nodos EKS |
| `ecr_repository_names` | Nombres de repositorios ECR | Referencia de imágenes |
| `ecr_repository_urls` | URLs completas de ECR | `docker tag/push` de imágenes |
| `ecr_docker_tags` | Comandos docker tag listos | Facilita el paso de imágenes |
| `eks_cluster_name` | Nombre del clúster EKS | `kubectl` y `helm` |
| `eks_cluster_endpoint` | Endpoint del API server | Configuración de kubectl |
| `kubeconfig_command` | Comando para obtener kubeconfig | `aws eks update-kubeconfig` |
| `eks_oidc_provider_arn` | ARN del OIDC provider | IRSA |

```bash
# Después de terraform apply, obtén los valores
terraform output ecr_repository_urls
# → {"api": "123456789012.dkr.ecr.us-east-1.amazonaws.com/softwarelearningguide-api:latest", ...}

terraform output kubeconfig_command
# → aws eks --region us-east-1 update-kubeconfig --name softwarelearningguide-dev-eks --alias softwarelearningguide-dev-eks
```

---

## Flujo de trabajo: comandos esenciales

### Comandos básicos de Terraform

```bash
# Inicializar (descarga providers y módulos) — ejecutar UNA sola vez
terraform init

# Formatear código HCL (canonical style)
terraform fmt -recursive

# Validar sintaxis y referencias (no contacta AWS)
terraform validate

# Ver el plan de cambios (no crea nada)
terraform plan -var-file="terraform.tfvars"

# Aplicar los cambios
terraform apply -var-file="terraform.tfvars"

# Destruir toda la infraestructura
terraform destroy -var-file="terraform.tfvars" -auto-approve

# Ver el estado actual
terraform show

# Importar recursos existentes a Terraform
terraform import aws_vpc.main vpc-12345678
```

### Flujo típico de desarrollo

```bash
# Ciclo de desarrollo rápido (contra MiniStack)
terraform init                                  # Una vez
terraform fmt -recursive                        # Formatear
terraform validate                              # Validar sintaxis
terraform plan -var-file="terraform.minstack.tfvars.example"  # Ver plan
terraform apply -var-file="terraform.minstack.tfvars.example"  # Aplicar
# ... probar con helm ...
terraform destroy -var-file="terraform.minstack.tfvars.example" -auto-approve  # Limpiar
```

---

## Buenas prácticas y seguridad

### Buenas prácticas aplicadas en este proyecto

- **Estado local en desarrollo**: `terraform.tfstate` vive en la máquina local. Para producción, usa un **backend remoto** (S3 + DynamoDB) para evitar el problema del estado compartido.
- **`.terraform/` en `.gitignore`**: El directorio `.terraform/` (donde se descargan los providers) está en `.gitignore`. **Sí se debe hacer commit del `.terraform.lock.hcl`** que fija las versiones exactas de los providers.
- **Variables parametrizadas**: Todo está en `variables.tf` con valores por defecto razonables. No hay "números mágicos" en los archivos HCL.
- **Módulos separados**: VPC, ECR y EKS son módulos independientes. Cada uno se puede probar y versionar por separado.
- **Skip credentials**: Los flags `skip_credentials_validation` y `skip_requesting_account_id` permiten hacer `plan` sin credenciales, lo que es esencial para CI/CD y emulación local.

### Seguridad

- **Credenciales fuera del repo**: Las credenciales reales nunca se ponen en el código. Se resuelven desde el entorno (`~/.aws/credentials`) o el rol de IAM.
- **MiniStack = credenciales dummy**: `test`/`test` solo funciona contra MiniStack. Nunca uses estas credenciales contra AWS real.
- **Principio de mínimo privilegio**: En producción, los `admin_arns` de EKS solo conceden los permisos necesarios.
- **Sin `force_delete` en producción**: `ecr_force_delete = true` está en los defaults de dev. En producción, ponlo a `false` para evitar borrar accidentalmente repositorios con imágenes.

---

## Documentación relacionada

| Tema | Documento |
|------|-----------|
| **Helm (despliegue de aplicaciones en Kubernetes)** | [`README.Helm.md`](README.Helm.md) |
| **MiniStack (emulador local de AWS)** | [`README.Ministack.md`](README.Ministack.md) |
| **AWS general (catálogo de servicios)** | [`README.AWS.md`](README.AWS.md) |
| **Despliegue completo (IaC + Helm)** | [`README.Deploy.md`](README.Deploy.md) |
| **Configuración de la API** | [`SoftwareLearningGuide.Api/README.Api.md`](../SoftwareLearningGuide.Api/README.Api.md) |
| **Docker Compose local** | [`README.md`](../../../README.md) |

---

**Feliz aprendizaje de IaC!** 🚀
