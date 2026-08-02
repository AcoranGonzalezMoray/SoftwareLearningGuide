<h1 align="center">AWS — Amazon Web Services</h1>

<p align="center">
  <em>Guía completa: qué es AWS, catálogo de servicios, precios y comandos AWS CLI por servicio</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/AWS-FF9900?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS">
  <img src="https://img.shields.io/badge/200%2B_Servicios-232F3E?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="200+ Servicios">
  <img src="https://img.shields.io/badge/Pay_as_you_go-00A1D6?style=for-the-badge" alt="Pay as you go">
  <img src="https://img.shields.io/badge/AWS_CLI-3B82F6?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS CLI">
</p>

---

## Tabla de Contenidos

1. [¿Qué es AWS?](#qué-es-aws)
2. [Infraestructura global: regiones y zonas de disponibilidad](#infraestructura-global-regiones-y-zonas-de-disponibilidad)
3. [Modelo de precios de AWS](#modelo-de-precios-de-aws)
4. [AWS Free Tier (nivel gratuito)](#aws-free-tier-nivel-gratuito)
5. [AWS CLI: instalación y configuración](#aws-cli-instalación-y-configuración)
6. [Categorías de servicios](#categorías-de-servicios)
7. [Computo](#cómputo)
   - [EC2](#ec2)
   - [AWS Lambda](#aws-lambda)
   - [Amazon ECS](#amazon-ecs)
   - [Amazon EKS](#amazon-eks)
   - [AWS Fargate](#aws-fargate)
   - [Elastic Beanstalk](#elastic-beanstalk)
8. [Almacenamiento](#almacenamiento)
   - [S3](#s3)
   - [EBS](#ebs)
   - [EFS](#efs)
   - [S3 Glacier](#s3-glacier)
9. [Bases de datos](#bases-de-datos)
   - [Amazon RDS](#amazon-rds)
   - [Amazon Aurora](#amazon-aurora)
   - [DynamoDB](#dynamodb)
   - [ElastiCache](#elasticache)
   - [Amazon Redshift](#amazon-redshift)
10. [Redes y entrega de contenido](#redes-y-entrega-de-contenido)
    - [Amazon VPC](#amazon-vpc)
    - [Route 53](#route-53)
    - [CloudFront](#cloudfront)
    - [API Gateway](#api-gateway)
    - [Elastic Load Balancing](#elastic-load-balancing)
    - [NAT Gateway](#nat-gateway)
11. [Mensajeria e integracion](#mensajería-e-integración)
    - [SQS](#sqs)
    - [SNS](#sns)
    - [EventBridge](#eventbridge)
    - [Kinesis](#kinesis)
    - [Step Functions](#step-functions)
12. [Seguridad, identidad y cumplimiento](#seguridad-identidad-y-cumplimiento)
    - [IAM](#iam)
    - [AWS KMS](#aws-kms)
    - [Secrets Manager](#secrets-manager)
    - [SSM Parameter Store](#ssm-parameter-store)
    - [Amazon Cognito](#amazon-cognito)
    - [AWS WAF](#aws-waf)
13. [Gestion y gobierno](#gestión-y-gobierno)
    - [CloudWatch](#cloudwatch)
    - [CloudTrail](#cloudtrail)
    - [CloudFormation](#cloudformation)
    - [AWS Config](#aws-config)
    - [AWS Budgets](#aws-budgets)
14. [Analitica](#analítica)
    - [Athena](#athena)
    - [AWS Glue](#aws-glue)
    - [Amazon EMR](#amazon-emr)
15. [Machine Learning e IA](#machine-learning-e-ia)
    - [Amazon SageMaker](#amazon-sagemaker)
    - [Amazon Bedrock](#amazon-bedrock)
16. [Herramientas de desarrollo y DevOps](#herramientas-de-desarrollo-y-devops)
17. [Servicios usados en este proyecto](#servicios-usados-en-este-proyecto)
18. [Referencias](#referencias)

---

## ¿Qué es AWS?

**Amazon Web Services (AWS)** es la plataforma de computación en la nube más grande y adoptada del mundo. Lanzada en 2006, ofrece **más de 200 servicios** que van desde infraestructura básica (servidores, almacenamiento, redes) hasta inteligencia artificial, machine learning, IoT, analítica y servicios de cómputo cuántico.

En lugar de comprar y mantener servidores físicos, con AWS alquilas recursos informáticos por demanda, pagando **solo por lo que usas**, sin contratos a largo plazo y con la capacidad de escalar de 0 a miles de servidores en minutos.

| Característica | Detalle |
|----------------|---------|
| **Lanzamiento** | 2006 |
| **Servicios** | Más de 200 servicios en 30+ categorías |
| **Regiones** | 30+ regiones y 90+ zonas de disponibilidad en todo el mundo |
| **Modelo de pago** | Pay-as-you-go (paga por uso), por segundo u hora |
| **Cuota de mercado** | Líder del mercado cloud (~30% de cuota global) |
| **Nivel gratuito** | Free Tier: siempre gratis, 12 meses gratis y pruebas gratis |

### ¿Qué problemas resuelve?

| Problema tradicional | Solución AWS |
|----------------------|--------------|
| Comprar servidores con meses de antelación | **EC2 / Lambda**: provisionas capacidad en minutos |
| Mantener y parchear infraestructura | **Servicios gestionados**: AWS gestiona el hardware, parches y alta disponibilidad |
| Picos de tráfico impredecibles | **Auto Scaling**: escala automáticamente según la demanda |
| Altos costes iniciales (CapEx) | **Pay-as-you-go**: convierte coste fijo en variable (OpEx) |
| Backup y disaster recovery complejos | **Snapshots, RDS Multi-AZ, S3 replication** integrados |
| Seguridad y cumplimiento | **IAM, KMS, CloudTrail, WAF** con certificaciones ISO, SOC, HIPAA |

### Ventajas clave

- **Elasticidad**: escalas hacia arriba o hacia abajo automáticamente según la carga.
- **Fiabilidad**: infraestructura redundante en múltiples zonas de disponibilidad.
- **Seguridad**: cifrado, identidad gestionada y auditoría por defecto.
- **Coste**: solo pagas por el uso real, con descuentos por compromiso.
- **Innovación**: nuevos servicios y features lanzados casi a diario.

---

## Infraestructura global: regiones y zonas de disponibilidad

AWS opera **centros de datos** organizados de forma jerárquica:

```
Región (us-east-1, eu-west-1, ap-southeast-1...)
└── Zonas de Disponibilidad (AZs): 3-6 por región
    └── Centros de datos aislados entre sí
        └── Servidores, almacenamiento y redes redundantes
```

- **Región**: área geográfica (ej: `us-east-1` N. Virginia, `eu-west-1` Irlanda, `sa-east-1` São Paulo). Cada región es totalmente independiente.
- **Zona de Disponibilidad (AZ)**: uno o más centros de datos aislados (energía, red y refrigeración independientes) dentro de una región. Las AZs están conectadas por fibra de baja latencia.
- **Punto de presencia (Edge)**: red global de CloudFront para entrega de contenido con baja latencia.

> **Regla práctica:** despliega tus aplicaciones en **al menos 2 AZs** de la misma región para alta disponibilidad. Elige la región según la **proximidad a tus usuarios**, requisitos legales de residencia de datos y **precios** (no todas las regiones cuestan igual).

```bash
# Listar regiones y AZs disponibles
aws ec2 describe-regions
aws ec2 describe-availability-zones --region us-east-1
```

---

## Modelo de precios de AWS

AWS cobra según una combinación de **cuatro ejes**:

| Eje de coste | Ejemplos | Unidad típica |
|--------------|----------|---------------|
| **Tiempo de cómputo** | EC2, Lambda, EKS | Por segundo / por hora / por GB-segundo |
| **Almacenamiento** | S3, EBS, RDS | Por GB al mes |
| **Red (transferencia)** | Datos de salida, NAT Gateway, CloudFront | Por GB transferido |
| **Peticiones (API)** | DynamoDB, SQS, SNS, API Gateway | Por millón de peticiones |

### Modos de pago

| Modalidad | Qué es | Descuento |
|-----------|--------|-----------|
| **On-Demand** | Pagas lo que usas, sin compromiso | Precio base |
| **Savings Plans** | Compromiso de uso ($/h) por 1-3 años | 30-60% (1 año), 50-72% (3 años) |
| **Reserved Instances** | Instancias reservadas por 1-3 años | Hasta 72% |
| **Spot Instances** | Capacidad sobrante de AWS a precio variable | 60-90% (pueden reclamarse con 2 min de aviso) |
| **Free Tier** | Uso gratuito limitado | 100% |

### Costes ocultos que más sorprenden

- **Transferencia de datos de salida**: `$0.09/GB` (primeros 10 TB) en `us-east-1`. Es el coste más subestimado.
- **NAT Gateway**: `$0.045/hora` + `$0.045/GB` — solo el coste fijo son ~`$32.85/mes` por gateway.
- **EBS snapshots**: `$0.05/GB-mes`.
- **CloudWatch Logs**: `$0.50/GB` ingerido + `$0.03/GB` almacenado.
- **Load Balancers**: coste por hora + por LCU, aunque no tengan tráfico.

> **Consejo:** usa la [AWS Pricing Calculator](https://calculator.aws/) antes de desplegar algo. Calculadora y presupuestos en [AWS Budgets](#aws-budgets).

---

## AWS Free Tier (nivel gratuito)

| Categoría | Qué incluye | Duración |
|-----------|-------------|----------|
| **Siempre gratis** | 1M peticiones Lambda/mes, 25 GB DynamoDB, 1M mensajes SQS/mes, 1 TB CloudFront/mes, IAM/VPC/Auto Scaling (todo gratis), 10.000 parámetros SSM estándar | Sin límite |
| **12 meses gratis** | 750 h/mes de EC2 `t2/t3.micro`, 5 GB S3 Standard, 750 h/mes de RDS `db.t2/t3.micro`, 100 GB de salida/mes | Desde el alta |
| **Pruebas gratis** | 1 mes de pruebas de productos: Redshift, SageMaker, EMR, etc. | 30 días |

```bash
# Ver tu uso del Free Tier y costes
aws ce get-usage-for-ec2-reserved-instances   # (reserved instances)
aws ce get-cost-and-usage --time-period Start=$(Get-Date -Format yyyy-MM-01),End=$(Get-Date -AddMonths 1 -Format yyyy-MM-01) --granularity MONTHLY --metrics "UnblendedCost"
```

---

## AWS CLI: instalación y configuración

La **AWS CLI** es la herramienta de línea de comandos para interactuar con todos los servicios de AWS. Es imprescindible para automatización, scripting y despliegues.

```bash
# Instalación (Windows / macOS / Linux)
# - Windows: descarga el instalador MSI desde https://aws.amazon.com/cli/
# - macOS: brew install awscli
# - Linux: curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"

# Verificar instalación
aws --version

# Configuración inicial (Access Key, Secret Key, Región, Formato de salida)
aws configure

# Configurar varios perfiles (ej: dev, prod, local)
aws configure --profile prod

# Ver identidad con la que estás autenticado
aws sts get-caller-identity
```

### Conceptos clave del CLI

| Concepto | Descripción |
|----------|-------------|
| `--region` | Región sobre la que se ejecuta el comando |
| `--profile` | Perfil de credenciales a usar |
| `--output json\|table\|text` | Formato de salida |
| `--endpoint-url` | Apuntar a un endpoint distinto (usado por MiniStack/LocalStack) |
| `--query` | Filtrar la salida JSON (JMESPath) |
| `--dry-run` | Validar el comando sin ejecutarlo |

```bash
# Comandos útiles transversales
aws help                              # Ayuda general
aws <servicio> help                   # Ayuda de un servicio (ej: aws s3 help)
aws ec2 describe-regions --output table
aws sts get-caller-identity
```

> **Para desarrollarlo en local sin cuenta real:** MiniStack emula los servicios de AWS en `http://localhost:4566`. Usa `--endpoint-url=http://localhost:4566` en cada comando. Ver [README.Ministack.md](README.Ministack.md).

---

## Categorías de servicios

AWS agrupa sus servicios en categorías según la función que cumplen:

| Categoría | Función | Ejemplos |
|-----------|---------|----------|
| **Cómputo** | Potencia de procesamiento (VMs, contenedores, serverless) | EC2, Lambda, ECS, EKS, Fargate |
| **Almacenamiento** | Guardar y recuperar datos | S3, EBS, EFS, Glacier |
| **Bases de datos** | Bases de datos relacionales y NoSQL | RDS, Aurora, DynamoDB, Redshift |
| **Redes y CDN** | Conectividad, DNS, balanceo de carga, CDN | VPC, Route 53, CloudFront, API Gateway, ELB |
| **Mensajería e integración** | Comunicación entre servicios | SQS, SNS, EventBridge, Kinesis, Step Functions |
| **Seguridad e identidad** | Autenticación, cifrado, permisos | IAM, KMS, Secrets Manager, Cognito, WAF |
| **Gestión y gobierno** | Monitorización, auditoría, orquestación | CloudWatch, CloudTrail, CloudFormation, Config |
| **Analítica** | Procesamiento y análisis de datos | Athena, Glue, EMR, Kinesis |
| **Machine Learning / IA** | Modelos y aplicaciones de IA | SageMaker, Bedrock |
| **Herramientas de desarrollo** | CI/CD, repositorios, despliegues | CodeCommit, CodeBuild, CodePipeline, CodeDeploy |

A continuación, cada categoría con sus servicios principales: **qué es**, **para qué se usa**, **cuánto cuesta** y **cómo se maneja con la AWS CLI**.

---

## Cómputo

### EC2

> **Amazon Elastic Compute Cloud** — servidores virtuales (VMs) en la nube, el servicio fundacional de AWS.

- **¿Qué es?** Máquinas virtuales con cientos de tipos de instancia optimizados para cómputo, memoria, GPU o almacenamiento.
- **Casos de uso:** servidores web, bases de datos auto-gestionadas, procesamiento por lotes, entornos de desarrollo.
- **Precio:** desde `$0.0042/h` (`t4g.nano` Graviton) o `$0.0104/h` (`t3.micro` x86). Spot hasta 90% más barato.

```bash
# Lanzar una instancia (Windows Server / Ubuntu / Amazon Linux)
aws ec2 run-instances --image-id ami-0abcdef1234567890 --instance-type t3.micro --key-name mi-clave --security-group-ids sg-123 --subnet-id subnet-123

# Listar instancias y su estado
aws ec2 describe-instances --query 'Reservations[].Instances[].[InstanceId,State.Name,PublicIpAddress]' --output table

# Detener / iniciar / reiniciar / terminar (¡cuidado con terminar!)
aws ec2 stop-instances --instance-ids i-1234567890abcdef0
aws ec2 start-instances --instance-ids i-1234567890abcdef0
aws ec2 reboot-instances --instance-ids i-1234567890abcdef0
aws ec2 terminate-instances --instance-ids i-1234567890abcdef0

# Par de claves para acceso SSH/RDP
aws ec2 create-key-pair --key-name mi-clave --query 'KeyMaterial' --output text > mi-clave.pem
aws ec2 describe-key-pairs
aws ec2 delete-key-pair --key-name mi-clave

# Grupos de seguridad (firewall)
aws ec2 create-security-group --group-name web --description "Trafico web" --vpc-id vpc-123
aws ec2 authorize-security-group-ingress --group-id sg-123 --protocol tcp --port 80 --cidr 0.0.0.0/0
aws ec2 describe-security-groups

# Snapshots (copias de seguridad del disco)
aws ec2 create-snapshot --volume-id vol-123 --description "Backup diario"
aws ec2 describe-snapshots
```

### AWS Lambda

> **Computación serverless** — ejecuta código sin gestionar servidores. Solo pagas por la ejecución.

- **¿Qué es?** Ejecuta funciones de código (Node, Python, Java, Go, .NET, etc.) en respuesta a eventos, sin aprovisionar servidores.
- **Casos de uso:** APIs serverless, procesamiento de eventos, tareas programadas (cron), integraciones, microservicios.
- **Precio:** `$0.20/1M` peticiones + `$0.0000166667/GB-segundo`. Primer millón de peticiones y 400.000 GB-s **gratis** cada mes.

```bash
# Empaquetar el código y crear la función
aws lambda create-function \
  --function-name mi-funcion \
  --runtime python3.12 \
  --role arn:aws:iam::123456789012:role/lambda-basic-execution \
  --handler lambda_function.lambda_handler \
  --zip-file fileb://function.zip

# Invocar
aws lambda invoke --function-name mi-funcion --payload '{"key":"value"}' respuesta.json

# Listar / actualizar / borrar
aws lambda list-functions
aws lambda update-function-code --function-name mi-funcion --zip-file fileb://function.zip
aws lambda update-function-configuration --function-name mi-funcion --memory-size 512
aws lambda delete-function --function-name mi-funcion

# Ver logs de las invocaciones
aws logs tail /aws/lambda/mi-funcion --follow
```

### Amazon ECS

> **Elastic Container Service** — ejecuta contenedores Docker sin gestionar Kubernetes.

- **¿Qué es?** Orquestador de contenedores gestionado. Ejecutas tareas y servicios en clústeres de EC2 o con Fargate.
- **Casos de uso:** microservicios en contenedores, aplicaciones 12-factor, despliegues blue/green.
- **Precio:** el plano de control es **gratis**; pagas el cómputo (EC2 o Fargate).

```bash
# Crear un clúster
aws ecs create-cluster --cluster-name mi-cluster
aws ecs list-clusters

# Registrar una definición de tarea (la "receta" del contenedor)
aws ecs register-task-definition \
  --family mi-app \
  --cpu 256 --memory 512 \
  --network-mode awsvpc \
  --container-definitions '[{"name":"app","image":"nginx:latest","portMappings":[{"containerPort":80}]}]'

# Ejecutar una tarea puntual
aws ecs run-task --cluster mi-cluster --task-definition mi-app --count 1

# Crear un servicio (tarea que se mantiene corriendo)
aws ecs create-service \
  --cluster mi-cluster \
  --service-name mi-servicio \
  --task-definition mi-app \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-123],securityGroups=[sg-123]}"

# Ver servicios y tareas
aws ecs list-services --cluster mi-cluster
aws ecs list-tasks --cluster mi-cluster
```

### Amazon EKS

> **Elastic Kubernetes Service** — Kubernetes gestionado por AWS.

- **¿Qué es?** Kubernetes como servicio: AWS gestiona el plano de control; tú gestionas los nodos.
- **Casos de uso:** cargas que ya usan Kubernetes, ecosistema CNCF, workloads que necesitan control fino.
- **Precio:** `$0.10/h` por clúster (~`$73/mes`) + nodos EC2 o Fargate.

```bash
# Crear un clúster EKS
aws eks create-cluster --name mi-cluster --role-arn arn:aws:iam::123456789012:role/eks-cluster-role --resources-vpc-config subnetIds=subnet-123,subnet-456
aws eks list-clusters
aws eks describe-cluster --name mi-cluster

# Configurar kubectl para hablar con el clúster
aws eks update-kubeconfig --region us-east-1 --name mi-cluster
kubectl get nodes
```

### AWS Fargate

> **Motor serverless para contenedores** — ejecuta contenedores sin gestionar servidores ni nodos.

- **¿Qué es?** Capa de cómputo para ECS y EKS que elimina la gestión de nodos. Defines CPU/memoria y Fargate lo ejecuta.
- **Precio:** `$0.04048/vCPU/h` + `$0.004445/GB/h`.

```bash
# Fargate se usa a través de ECS (launch-type FARGATE) o EKS
aws ecs create-service --launch-type FARGATE ...   # ver ejemplo en ECS

# Ver tareas Fargate
aws ecs list-tasks --cluster mi-cluster
```

### Elastic Beanstalk

> **PaaS de AWS** — despliega aplicaciones sin pensar en la infraestructura.

- **¿Qué es?** Servicio que aprovisiona automáticamente (EC2, ALB, Auto Scaling, RDS) según una plataforma elegida. Ideal para empezar rápido.
- **Precio:** gratis; pagas solo por los recursos subyacentes.

```bash
# Crear y desplegar una aplicación
aws elasticbeanstalk create-application --application-name mi-app
aws elasticbeanstalk create-environment \
  --application-name mi-app \
  --environment-name mi-app-prod \
  --solution-stack-name "64bit Amazon Linux 2023 v6.1.0 running Python 3.11"
# (desplegar código: eb deploy desde el CLI de Beanstalk)

# Ver entornos y estado
aws elasticbeanstalk describe-environments
aws elasticbeanstalk list-platform-versions
```

---

## Almacenamiento

### S3

> **Simple Storage Service** — almacenamiento de objetos (archivos, imágenes, backups, logs) con durabilidad del 99.999999999% (11 nueves).

- **¿Qué es?** Guarda "objetos" (archivos) dentro de "buckets" (contenedores), organizados por "claves" (rutas). Sin límite de tamaño.
- **Casos de uso:** hosting estático, data lakes, backups, distribución de medios, almacenamiento de logs.
- **Precio:** `$0.023/GB-mes` (Standard, primeros 50 TB). Requests: `$0.005/1k` PUT, `$0.0004/1k` GET.

```bash
# Crear bucket (los nombres son globales y únicos)
aws s3 mb s3://mi-bucket --region us-east-1
aws s3 ls                          # listar buckets

# Copiar / mover / borrar / sincronizar
aws s3 cp archivo.txt s3://mi-bucket/
aws s3 cp s3://mi-bucket/archivo.txt ./
aws s3 mv s3://mi-bucket/archivo.txt s3://mi-bucket/renombrado.txt
aws s3 rm s3://mi-bucket/archivo.txt
aws s3 sync ./carpeta s3://mi-bucket/carpeta
aws s3 ls s3://mi-bucket --recursive

# Generar URL prefirmada (acceso temporal a un objeto)
aws s3 presign s3://mi-bucket/archivo.txt --expires-in 3600

# API avanzada (versionado, políticas, lifecycle)
aws s3api create-bucket --bucket mi-bucket --region us-east-1
aws s3api put-bucket-versioning --bucket mi-bucket --versioning-configuration Status=Enabled
aws s3api put-bucket-policy --bucket mi-bucket --policy file://policy.json
aws s3api put-bucket-lifecycle-configuration --bucket mi-bucket --lifecycle-configuration file://lifecycle.json
aws s3api get-bucket-encryption --bucket mi-bucket
```

### EBS

> **Elastic Block Store** — discos de bloques persistentes que se conectan a instancias EC2.

- **¿Qué es?** Volúmenes de almacenamiento tipo SSD/HDD conectados a una EC2 (como un disco duro virtual).
- **Casos de uso:** sistema de archivos de instancias, bases de datos auto-gestionadas, volúmenes de arranque.
- **Precio:** `$0.08/GB-mes` (`gp3`, 3.000 IOPS y 125 MB/s incluidos). Snapshots `$0.05/GB-mes`.

```bash
# Crear un volumen y adjuntarlo a una instancia
aws ec2 create-volume --availability-zone us-east-1a --size 100 --volume-type gp3
aws ec2 attach-volume --volume-id vol-123 --instance-id i-123 --device /dev/sdf

# Listar / modificar / borrar
aws ec2 describe-volumes
aws ec2 modify-volume --volume-id vol-123 --size 200
aws ec2 delete-volume --volume-id vol-123

# Snapshots
aws ec2 create-snapshot --volume-id vol-123 --description "Backup"
aws ec2 describe-snapshots
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-123 --description "Copia"
aws ec2 delete-snapshot --snapshot-id snap-123
```

### EFS

> **Elastic File System** — sistema de archivos compartido (NFS) para múltiples instancias EC2 simultáneamente.

- **¿Qué es?** Almacenamiento de archivos gestionado, elástico y compartido. Se monta vía NFS.
- **Casos de uso:** almacenamiento compartido entre contenedores/microservicios, home directories, repositorios de código.
- **Precio:** desde `$0.30/GB-mes` (Standard). Sin coste por aprovisionamiento.

```bash
# Crear un sistema de archivos
aws efs create-file-system --creation-token mi-efs --performance-mode generalPurpose
aws efs describe-file-systems

# Crear mount targets (en cada AZ)
aws efs create-mount-target --file-system-id fs-123 --subnet-id subnet-123 --security-groups sg-123
aws efs describe-mount-targets --file-system-id fs-123

# Montar en una instancia EC2
# sudo mount -t nfs4 -o nfsvers=4.1,rsize=1048576,wsize=1048576 fs-123.efs.us-east-1.amazonaws.com:/ /mnt/efs
```

### S3 Glacier

> **Almacenamiento de archivo (cold storage)** — el más barato de AWS para datos que casi no se acceden.

- **¿Qué es?** Clases de almacenamiento dentro de S3 para datos de archivo con recuperación de minutos a horas.
- **Casos de uso:** archivos legales, auditorías, backups de retención larga, datos regulados.
- **Precio:** Glacier Flexible `$0.0036/GB-mes`; Glacier Deep Archive `$0.00099/GB-mes` (el más barato).

```bash
# Vaults (contenedores de archivo)
aws glacier create-vault --account-id - --vault-name mi-vault
aws glacier list-vaults --account-id -
aws glacier upload-archive --account-id - --vault-name mi-vault --body archivo.zip

# La forma moderna: usar S3 con clase de almacenamiento GLACIER
aws s3api put-object --bucket mi-bucket --key backup.zip --storage-class GLACIER
aws s3api copy-object --bucket mi-bucket --key backup.zip --copy-source mi-bucket/backup.zip --storage-class DEEP_ARCHIVE

# Recuperar un objeto de Glacier (restore)
aws s3api restore-object --bucket mi-bucket --key backup.zip --restore-request '{"Days":7,"GlacierJobParameters":{"Tier":"Standard"}}'
```

---

## Bases de datos

### Amazon RDS

> **Relational Database Service** — bases de datos relacionales gestionadas (MySQL, PostgreSQL, MariaDB, Oracle, SQL Server).

- **¿Qué es?** Despliega y gestiona automáticamente la base de datos: parches, backups, replicas y failover.
- **Casos de uso:** aplicaciones con datos relacionales, migraciones desde SQL Server/MySQL/Postgres on-premise, sistemas transaccionales.
- **Precio:** `db.t4g.micro` ~`$0.016/h`. **Multi-AZ** duplica el coste (alta disponibilidad).

```bash
# Crear una instancia de base de datos
aws rds create-db-instance \
  --db-instance-identifier mi-db \
  --db-instance-class db.t4g.micro \
  --engine mysql \
  --allocated-storage 20 \
  --master-username admin --master-user-password "MiPasswordSeguro!123"

# Ver / modificar / borrar
aws rds describe-db-instances
aws rds modify-db-instance --db-instance-identifier mi-db --db-instance-class db.t4g.small --apply-immediately
aws rds delete-db-instance --db-instance-identifier mi-db --skip-final-snapshot

# Snapshots y restauración
aws rds create-db-snapshot --db-instance-identifier mi-db --db-snapshot-identifier mi-db-backup
aws rds describe-db-snapshots
aws rds restore-db-instance-from-db-snapshot --db-instance-identifier mi-db-restaurada --db-snapshot-identifier mi-db-backup

# Replicas de lectura
aws rds create-db-instance-read-replica --db-instance-identifier mi-db-lectura --source-db-instance-identifier mi-db
```

### Amazon Aurora

> **Base de datos relacional compatible con MySQL/PostgreSQL**, 5x más rápida, a escala de cloud.

- **¿Qué es?** Motor de base de datos de AWS (compatible con MySQL y PostgreSQL) con alto rendimiento, replicación y escalado automático.
- **Casos de uso:** aplicaciones que necesitan rendimiento de alta disponibilidad con compatibilidad MySQL/Postgres.
- **Precio:** `$0.10/GB-mes` + `$0.20/1M` I/O. `Aurora Serverless v2` escala automáticamente.

```bash
# Crear un clúster Aurora (PostgreSQL o MySQL)
aws rds create-db-cluster \
  --db-cluster-identifier aurora-postgres \
  --engine aurora-postgresql \
  --engine-version 15.4 \
  --master-username admin --master-user-password "MiPasswordSeguro!123"

# Añadir instancias de escritura/lectura
aws rds create-db-instance --db-instance-identifier aurora-postgres-inst1 --db-cluster-identifier aurora-postgres --engine aurora-postgresql --db-instance-class db.r5.large

# Ver clústeres
aws rds describe-db-clusters
aws rds describe-db-instances --filters "Name=db-cluster-id,Values=aurora-postgres"
```

### DynamoDB

> **Base de datos NoSQL serverless** de clave-valor y documentos, con latencia de milisegundos de un solo dígito.

- **¿Qué es?** Base de datos NoSQL completamente gestionada que escala automáticamente sin servidores.
- **Casos de uso:** sesiones de usuario, carritos, tablas de lookups, IoT, aplicaciones de alta escala.
- **Precio:** On-demand `$0.25/1M` lecturas y `$1.25/1M` escrituras. **25 GB de almacenamiento siempre gratis.**

```bash
# Crear tabla
aws dynamodb create-table \
  --table-name Usuarios \
  --attribute-definitions AttributeName=id,AttributeType=S \
  --key-schema AttributeName=id,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST
aws dynamodb list-tables

# CRUD
aws dynamodb put-item --table-name Usuarios --item '{"id":{"S":"1"},"nombre":{"S":"Ana"},"edad":{"N":"30"}}'
aws dynamodb get-item --table-name Usuarios --key '{"id":{"S":"1"}}'
aws dynamodb update-item --table-name Usuarios --key '{"id":{"S":"1"}}' --update-expression "SET edad = :n" --expression-attribute-values '{":n":{"N":"31"}}'
aws dynamodb delete-item --table-name Usuarios --key '{"id":{"S":"1"}}'

# Query y Scan
aws dynamodb query --table-name Usuarios --key-condition-expression "id = :id" --expression-attribute-values '{":id":{"S":"1"}}'
aws dynamodb scan --table-name Usuarios

# TTL (expiración automática)
aws dynamodb update-time-to-live --table-name Usuarios --time-to-live-specification "Enabled=true,AttributeName=expira"
```

### ElastiCache

> **Caché en memoria gestionada** — Redis, Valkyrie o Memcached como servicio.

- **¿Qué es?** Memoria caché distribuida gestionada por AWS.
- **Casos de uso:** caché de bases de datos, sesiones, rate limiting, colas ligeras, leaderboards.
- **Precio:** desde ~`$0.017/h` (`cache.t3.micro`).

```bash
# Crear un clúster de caché Redis
aws elasticache create-cache-cluster --cache-cluster-id mi-cache --cache-node-type cache.t3.micro --engine redis --num-cache-nodes 1
aws elasticache describe-cache-clusters

# Replication group (alta disponibilidad)
aws elasticache create-replication-group --replication-group-id mi-cache-ha --replication-group-description "Redis HA" --cache-node-type cache.t3.micro --engine redis --num-cache-clusters 2

# Verificar y borrar
aws elasticache describe-replication-groups
aws elasticache delete-cache-cluster --cache-cluster-id mi-cache
```

### Amazon Redshift

> **Data warehouse** para analítica a gran escala sobre petabytes de datos.

- **¿Qué es?** Almacén de datos columnar optimizado para consultas analíticas complejas sobre grandes volúmenes.
- **Casos de uso:** reporting, BI, data warehousing, análisis histórico.
- **Precio:** desde ~`$0.25/h` (nodos densos). Pago por nodo + almacenamiento.

```bash
# Crear un clúster
aws redshift create-cluster \
  --cluster-identifier mi-wh \
  --node-type dc2.large \
  --number-of-nodes 2 \
  --master-username admin --master-user-password "MiPasswordSeguro!123"

aws redshift describe-clusters
aws redshift delete-cluster --cluster-identifier mi-wh --skip-final-cluster-snapshot

# Ejecutar SQL (usando el endpoint de la DB)
# psql -h <endpoint.redshift.amazonaws.com> -U admin -d dev
```

---

## Redes y entrega de contenido

### Amazon VPC

> **Virtual Private Cloud** — tu red privada aislada dentro de AWS.

- **¿Qué es?** Red virtual propia con IPs, subredes, tablas de rutas, gateways y firewalls. Todo se despliega dentro de una VPC.
- **Casos de uso:** aislar entornos, controlar el tráfico entrante/saliente, conectar con on-premise (VPN/Direct Connect).
- **Precio:** **gratis** (solo pagas por componentes como NAT Gateway o VPN).

```bash
# Red base
aws ec2 create-vpc --cidr-block 10.0.0.0/16
aws ec2 create-subnet --vpc-id vpc-123 --cidr-block 10.0.1.0/24 --availability-zone us-east-1a
aws ec2 create-internet-gateway
aws ec2 attach-internet-gateway --internet-gateway-id igw-123 --vpc-id vpc-123

# Tabla de rutas: dar salida a Internet a las subredes públicas
aws ec2 create-route-table --vpc-id vpc-123
aws ec2 create-route --route-table-id rtb-123 --destination-cidr-block 0.0.0.0/0 --gateway-id igw-123
aws ec2 associate-route-table --route-table-id rtb-123 --subnet-id subnet-123

# NAT Gateway (salida a Internet para subredes privadas, sin IP pública)
aws ec2 allocate-address --domain vpc
aws ec2 create-nat-gateway --subnet-id subnet-123 --allocation-id eipalloc-123

# VPC Endpoints (conexión privada a S3/DynamoDB sin pasar por Internet)
aws ec2 create-vpc-endpoint --vpc-id vpc-123 --service-name com.amazonaws.us-east-1.s3 --route-table-ids rtb-123

# Peering entre VPCs
aws ec2 create-vpc-peering-connection --vpc-id vpc-123 --peer-vpc-id vpc-456
```

### Route 53

> **DNS gestionado** — registra dominios y resuelve nombres de dominio.

- **¿Qué es?** Servicio DNS escalable y registro de dominios.
- **Casos de uso:** DNS de alta disponibilidad, balanceo por latencia, failover, health checks, registrar dominios.
- **Precio:** `$0.50/hosted-zone/mes` + coste por consulta (`~$0.40-0.60/M`).

```bash
# Crear una hosted zone
aws route53 create-hosted-zone --name midominio.com --caller-reference ref-001
aws route53 list-hosted-zones

# Añadir registros DNS (A, CNAME, MX, TXT, etc.)
aws route53 change-resource-record-sets \
  --hosted-zone-id Z123456789 \
  --change-batch '{"Changes":[{"Action":"UPSERT","ResourceRecordSet":{"Name":"www.midominio.com","Type":"A","TTL":300,"ResourceRecords":[{"Value":"1.2.3.4"}]}}]}'

# Ver registros y cambios
aws route53 list-resource-record-sets --hosted-zone-id Z123456789
aws route53 get-change --id C123456789

# Health check
aws route53 create-health-check --caller-reference hc-001 --health-check-config '{"Type":"HTTP","IPAddress":"1.2.3.4","Port":80}'
```

### CloudFront

> **CDN (Content Delivery Network)** — distribuye contenido estático y dinámico a nivel global.

- **¿Qué es?** Red de borde que cachea y sirve contenido desde los puntos de presencia más cercanos al usuario.
- **Casos de uso:** acelerar sitios web, servir media, APIs globales, proteger con WAF, SSL gestionado.
- **Precio:** `$0.085/GB` de salida a Internet (1 TB gratis cada mes).

```bash
# Crear una distribución (con origen en S3)
aws cloudfront create-distribution \
  --distribution-config '{"CallerReference":"ref-001","Comment":"Mi CDN","Origins":{"Quantity":1,"Items":[{"Id":"S3-mi-bucket","DomainName":"mi-bucket.s3.amazonaws.com","S3OriginConfig":{"OriginAccessIdentity":""}}]},"DefaultCacheBehavior":{"TargetOriginId":"S3-mi-bucket","ViewerProtocolPolicy":"redirect-to-https","ForwardedValues":{"QueryString":false,"Cookies":{"Forward":"none"}},"MinTTL":0,"TrustedSigners":{"Enabled":false,"Quantity":0}},"Enabled":true}'

aws cloudfront list-distributions
aws cloudfront get-distribution --id E1234567890ABC

# Invalidar la caché
aws cloudfront create-invalidation --distribution-id E1234567890ABC --paths "/*"
aws cloudfront list-invalidations --distribution-id E1234567890ABC
```

### API Gateway

> **Crea y publica APIs** REST y HTTP de forma gestionada y escalable.

- **¿Qué es?** Punto de entrada gestionado para tus APIs. Se integra con Lambda, EC2, HTTP y más.
- **Casos de uso:** APIs serverless, capa de autenticación, rate limiting, versionado de APIs.
- **Precio:** REST `$3.50/M` peticiones; HTTP `$1.00/M`; WebSocket `$1.00/M` (conectores) + `$0.25/M` mensajes.

```bash
# API REST (v1)
aws apigateway create-rest-api --name "Mi API"
aws apigateway get-resources --rest-api-id abc123
aws apigateway put-method --rest-api-id abc123 --resource-id / --http-method GET --authorization-type NONE
aws apigateway put-integration --rest-api-id abc123 --resource-id / --http-method GET --type MOCK
aws apigateway create-deployment --rest-api-id abc123 --stage-name prod

# API HTTP (v2)
aws apigatewayv2 create-api --name "Mi HTTP API" --protocol-type HTTP
aws apigatewayv2 create-stage --api-id abcdef --stage-name prod
aws apigatewayv2 get-apis
```

### Elastic Load Balancing

> **Balanceo de carga** — distribuye tráfico entre instancias, contenedores o Lambdas.

- **¿Qué es?** ALB (nivel 7, HTTP/HTTPS), NLB (nivel 4, TCP/UDP), CLB (legacy).
- **Casos de uso:** distribuir tráfico web, exponer microservicios, failover automático.
- **Precio:** ALB `$0.0225/h` + `$0.008/LCU-h`; NLB `$0.0225/h` + `$0.006/LCU-h`.

```bash
# Crear un ALB
aws elbv2 create-load-balancer --name mi-alb --subnets subnet-123 subnet-456 --security-groups sg-123
aws elbv2 describe-load-balancers

# Target group (grupo de destinos)
aws elbv2 create-target-group --name mi-tg --protocol HTTP --port 80 --vpc-id vpc-123 --target-type instance
aws elbv2 register-targets --target-group-arn arn:aws:elasticloadbalancing:... --targets Id=i-123 Id=i-456

# Listener (regla de enrutado del tráfico)
aws elbv2 create-listener --load-balancer-arn arn:aws:elasticloadbalancing:... --protocol HTTP --port 80 --default-actions Type=forward,TargetGroupArn=arn:aws:elasticloadbalancing:...

# Reglas por path (ej: /api → otro target group)
aws elbv2 create-rule --listener-arn arn:aws:elasticloadbalancing:... --priority 10 --conditions Field=path-pattern,Values=['/api/*'] --actions Type=forward,TargetGroupArn=...
```

### NAT Gateway

> **Da salida a Internet** a las subredes privadas (hacia fuera) sin exponerlas.

- **¿Qué es?** Gateway de traducción de direcciones (NAT) gestionado para subredes privadas.
- **Precio:** `$0.045/h` + `$0.045/GB` procesado. ¡El coste fijo sorprende: ~`$32.85/mes` por gateway!

```bash
aws ec2 create-nat-gateway --subnet-id subnet-123 --allocation-id eipalloc-123
aws ec2 describe-nat-gateways
aws ec2 delete-nat-gateway --nat-gateway-id nat-123
```

---

## Mensajería e integración

### SQS

> **Simple Queue Service** — colas de mensajes gestionadas (desacopla servicios).

- **¿Qué es?** Colas de mensajes con modelo de cola estándar y FIFO. El mensaje se borra cuando el consumidor lo procesa (at-least-once).
- **Casos de uso:** desacoplar microservicios, buffers de peticiones, integraciones asíncronas, fans-out de trabajo.
- **Precio:** `$0.40/1M` peticiones (el primer millón es gratis cada mes).

```bash
# Crear cola
aws sqs create-queue --queue-name mi-cola
aws sqs list-queues
aws sqs get-queue-url --queue-name mi-cola

# Enviar / recibir / borrar mensajes
aws sqs send-message --queue-url https://sqs.us-east-1.amazonaws.com/123456789012/mi-cola --message-body "Hola mundo"
aws sqs send-message-batch --queue-url ... --entries '[{"Id":"1","MessageBody":"msg1"},{"Id":"2","MessageBody":"msg2"}]'
aws sqs receive-message --queue-url ... --max-number-of-messages 10 --wait-time-seconds 20
aws sqs delete-message --queue-url ... --receipt-handle "<ReceiptHandle>"
aws sqs purge-queue --queue-url ...   # ¡vaciar TODOS los mensajes!

# Atributos y configuración
aws sqs get-queue-attributes --queue-url ... --attribute-names All
aws sqs set-queue-attributes --queue-url ... --attributes '{"VisibilityTimeout":"60","MessageRetentionPeriod":"345600"}'

# Colas FIFO (orden garantizado)
aws sqs create-queue --queue-name mi-cola.fifo --attributes '{"FifoQueue":"true","ContentBasedDeduplication":"true"}'
```

### SNS

> **Simple Notification Service** — publica notificaciones a múltiples suscriptores (fan-out).

- **¿Qué es?** Publica mensajes a topicos; los suscriptores (SQS, Lambda, email, SMS, HTTP) los reciben.
- **Casos de uso:** alertas, notificaciones por email/SMS, fan-out de eventos a varias colas.
- **Precio:** `$0.50/1M` publicaciones (primer millón gratis). Email/SMS tienen costes adicionales.

```bash
# Crear un tópico
aws sns create-topic --name mi-topico
aws sns list-topics

# Suscribir destinos
aws sns subscribe --topic-arn arn:aws:sns:us-east-1:123456789012:mi-topico --protocol email --notification-endpoint yo@midominio.com
aws sns subscribe --topic-arn ... --protocol sqs --notification-endpoint arn:aws:sqs:us-east-1:123456789012:mi-cola
aws sns subscribe --topic-arn ... --protocol lambda --notification-endpoint arn:aws:lambda:us-east-1:123456789012:function:mi-funcion
aws sns list-subscriptions-by-topic --topic-arn ...

# Publicar
aws sns publish --topic-arn arn:aws:sns:us-east-1:123456789012:mi-topico --message "Alerta: CPU alta"
aws sns publish --topic-arn ... --message "{}" --message-structure json

# Borrar
aws sns unsubscribe --subscription-arn arn:aws:sns:us-east-1:123456789012:subscription...
aws sns delete-topic --topic-arn ...
```

### EventBridge

> **Bus de eventos** serverless — conecta aplicaciones con eventos de AWS y de terceros.

- **¿Qué es?** Recibe eventos (`put-events`) y los enruta mediante reglas a destinos (Lambda, SQS, SNS, Step Functions...).
- **Casos de uso:** arquitecturas event-driven, reaccionar a cambios de infraestructura, integraciones SaaS.
- **Precio:** `$1.00/1M` eventos personalizados (los eventos de servicios de AWS son gratis).

```bash
# Crear una regla que detecta un evento
aws events put-rule --name "InstanciaTerminada" --event-pattern '{"source":["aws.ec2"],"detail-type":["EC2 Instance State-change Notification"],"detail":{"state":["terminated"]}}'
aws events list-rules

# Poner un destino a la regla
aws events put-targets --rule InstanciaTerminada --targets '[{"Id":"1","Arn":"arn:aws:lambda:us-east-1:123456789012:function:mi-funcion"}]'

# Enviar un evento manualmente
aws events put-events --entries '[{"Source":"mi-app","DetailType":"UsuarioCreado","Detail":"{\"userId\":\"123\"}","EventBusName":"default"}]'

# Borrar
aws events remove-targets --rule InstanciaTerminada --ids 1
aws events delete-rule --name InstanciaTerminada
```

### Kinesis

> **Streaming de datos en tiempo real** — ingiere y procesa datos continuos.

- **¿Qué es?** Kinesis Data Streams recibe miles de registros por segundo y los procesa con consumidores (Lambda, KCL).
- **Casos de uso:** telemetría, logs en tiempo real, clickstreams, IoT, analítica en tiempo real.
- **Precio:** `$0.015/shard-hora` + `$0.014/1M` registros (PUT).

```bash
# Crear un stream
aws kinesis create-stream --stream-name mi-stream --shard-count 1
aws kinesis list-streams
aws kinesis describe-stream --stream-name mi-stream

# Escribir registros
aws kinesis put-record --stream-name mi-stream --partition-key usuario-1 --data "$(echo '{"evento":"click"}' | base64)"
aws kinesis put-records --stream-name mi-stream --records file://records.json

# Leer registros
aws kinesis get-shard-iterator --stream-name mi-stream --shard-id shardId-000000000000 --shard-iterator-type TRIM_HORIZON
aws kinesis get-records --shard-iterator <ShardIterator>

# Borrar
aws kinesis delete-stream --stream-name mi-stream
```

### Step Functions

> **Orquestación de flujos de trabajo** — coordina múltiples servicios en un workflow visual.

- **¿Qué es?** Máquina de estados (ASL) que orquesta Lambda, ECS, APIs, retries, paralelismo y pasos humanos.
- **Casos de uso:** workflows de aprobación, procesamiento ETL, orquestación de microservicios, sagas.
- **Precio:** `$0.025/1k` transiciones de estado (Standard).

```bash
# Crear una máquina de estados
aws stepfunctions create-state-machine \
  --name mi-flujo \
  --definition '{"StartAt":"Hola","States":{"Hola":{"Type":"Pass","Result":"Hola","End":true}}}' \
  --role-arn arn:aws:iam::123456789012:role/sfn-role
aws stepfunctions list-state-machines

# Ejecutar y monitorizar
aws stepfunctions start-execution --state-machine-arn arn:aws:states:us-east-1:123456789012:stateMachine:mi-flujo --input '{"clave":"valor"}'
aws stepfunctions list-executions --state-machine-arn arn:aws:states:...
aws stepfunctions describe-execution --execution-arn arn:aws:states:...
aws stepfunctions get-execution-history --execution-arn arn:aws:states:...

# Borrar
aws stepfunctions delete-state-machine --state-machine-arn arn:aws:states:...
```

---

## Seguridad, identidad y cumplimiento

### IAM

> **Identity and Access Management** — gestiona usuarios, roles y permisos. **Es el servicio de seguridad más importante de AWS.**

- **¿Qué es?** Controla quién (identidad) puede hacer qué (permiso) sobre qué (recurso).
- **Conceptos:** User (usuario humano), Role (identidad para servicios), Policy (documento JSON de permisos), Group.
- **Precio:** **gratis** (100%).

```bash
# Usuarios
aws iam create-user --user-name ana
aws iam list-users
aws iam create-access-key --user-name ana        # !!! guardar las claves
aws iam delete-user --user-name ana

# Roles
aws iam create-role --role-name lambda-role --assume-role-policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"lambda.amazonaws.com"},"Action":"sts:AssumeRole"}]}'
aws iam list-roles

# Políticas
aws iam create-policy --policy-name s3-read-only --policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":["s3:GetObject","s3:ListBucket"],"Resource":"*"}]}'
aws iam list-policies --scope Local
aws iam attach-role-policy --role-name lambda-role --policy-arn arn:aws:iam::123456789012:policy/s3-read-only
aws iam attach-user-policy --user-name ana --policy-arn arn:aws:iam::aws:policy/AmazonS3ReadOnlyAccess

# Ver y validar permisos
aws iam get-account-summary
aws iam simulate-principal-policy --policy-source-arn arn:aws:iam::123456789012:user/ana --action-names s3:GetObject s3:DeleteBucket
aws iam get-policy-version --policy-arn ... --version-id v1
```

> **Principio de mínimo privilegio:** da solo los permisos necesarios. Cada servicio del proyecto lee únicamente sus propios parámetros.

### AWS KMS

> **Key Management Service** — crea y gestiona claves de cifrado.

- **¿Qué es?** Servicio de gestión de claves de cifrado (CMK) usadas para cifrar datos en S3, EBS, RDS, SSM, Secrets Manager, etc.
- **Casos de uso:** cifrado en reposo, cifrado de secretos, firma de datos.
- **Precio:** `$1.00/clave/mes` + `$0.03/10k` peticiones de cifrado/descifrado.

```bash
# Crear una clave y un alias
aws kms create-key --description "Clave para mi-app"
aws kms create-alias --alias-name alias/mi-app --target-key-id <KeyId>
aws kms list-keys
aws kms describe-key --key-id alias/mi-app

# Cifrar y descifrar
aws kms encrypt --key-id alias/mi-app --plaintext "$(echo -n 'secreto' | base64)" --output text --query CiphertextBlob
aws kms decrypt --ciphertext-blob <CiphertextBlob> --output text --query Plaintext

# Rotación y borrado
aws kms enable-key-rotation --key-id alias/mi-app
aws kms schedule-key-deletion --key-id <KeyId> --pending-window-in-days 7
```

### Secrets Manager

> **Gestión de secretos** — almacena, rota y recupera credenciales y contraseñas de forma segura.

- **¿Qué es?** Guarda secretos (passwords, API keys, connection strings) con rotación automática integrada.
- **Casos de uso:** credenciales de base de datos, claves de API, tokens. Rotation automática.
- **Precio:** `$0.40/secreto/mes` + `$0.05/10k` llamadas API.

```bash
# Crear un secreto
aws secretsmanager create-secret --name prod/db/password --secret-string '{"username":"admin","password":"MiPasswordSeguro!123"}'
aws secretsmanager create-secret --name prod/api-key --secret-string "sk-123456789"

# Leer / listar / actualizar / rotar / borrar
aws secretsmanager get-secret-value --secret-id prod/db/password
aws secretsmanager list-secrets
aws secretsmanager update-secret --secret-id prod/api-key --secret-string "sk-987654321"
aws secretsmanager rotate-secret --secret-id prod/db/password --rotation-rules '{"AutomaticallyAfterDays":30}'
aws secretsmanager delete-secret --secret-id prod/api-key --force-delete-without-recovery
```

### SSM Parameter Store

> **Almacén de parámetros y configuración** — configuración centralizada y secretos sin coste.

- **¿Qué es?** Almacén de parámetros (config) dentro de Systems Manager. Se organiza en rutas jerárquicas tipo `/app/env/servicio/clave`.
- **Casos de uso:** connection strings, feature flags, config centralizada, versionado de parámetros.
- **Precio:** tier **Standard gratis** (sin límite de peticiones). Tier Advanced `$0.05/parámetro/mes`.

```bash
# Crear / actualizar parámetros
aws ssm put-parameter --name "/miapp/dev/ConnectionStrings/MyDb" --value "Server=...;Database=..." --type String
aws ssm put-parameter --name "/miapp/prod/MessageBroker/Password" --value "secreto" --type SecureString
aws ssm put-parameter --name "/miapp/dev/api/baseUrl" --value "https://api.dev" --type String --overwrite

# Leer
aws ssm get-parameter --name "/miapp/dev/api/baseUrl"
aws ssm get-parameter --name "/miapp/prod/MessageBroker/Password" --with-decryption
aws ssm get-parameters --names "/miapp/dev/api/baseUrl" "/miapp/dev/api/key" --with-decryption
aws ssm get-parameters-by-path --path "/miapp/dev/" --recursive --with-decryption

# Listar / historial / borrar
aws ssm describe-parameters
aws ssm get-parameter-history --name "/miapp/prod/MessageBroker/Password"
aws ssm delete-parameter --name "/miapp/dev/api/baseUrl"
aws ssm delete-parameters --names "/miapp/dev/api/baseUrl" "/miapp/dev/api/key"
```

### Amazon Cognito

> **Autenticación e identidad para aplicaciones** — sign-up, sign-in y control de acceso.

- **¿Qué es?** User Pools (registro/login de usuarios) + Identity Pools (credenciales temporales AWS). Incluye federación con Google, Facebook, etc.
- **Casos de uso:** autenticación de usuarios en apps móviles/web, OAuth2/OIDC, MFA.
- **Precio:** primeros **50.000 usuarios activos/mes gratis**; luego ~`$0.0055/MAU`.

```bash
# User Pool (base de usuarios)
aws cognito-idp create-user-pool --pool-name mi-pool
aws cognito-idp create-user-pool-client --user-pool-id <PoolId> --client-name web-app --no-generate-secret
aws cognito-idp list-user-pools --max-results 10

# Usuarios
aws cognito-idp admin-create-user --user-pool-id <PoolId> --username ana@mail.com --temporary-password "Temp!123"
aws cognito-idp admin-set-user-password --user-pool-id <PoolId> --username ana@mail.com --password "MiPasswordSeguro!123" --permanent
aws cognito-idp list-users --user-pool-id <PoolId>

# Identity Pool (credenciales AWS)
aws cognito-identity create-identity-pool --identity-pool-name mi-identidad --allow-unauthenticated-identities
aws cognito-identity list-identity-pools --max-results 10
```

### AWS WAF

> **Web Application Firewall** — protege tus aplicaciones web contra ataques (SQLi, XSS, DDoS).

- **¿Qué es?** Firewall de nivel de aplicación que filtra el tráfico antes de llegar a tu app (CloudFront, ALB, API Gateway).
- **Casos de uso:** bloquear IPs maliciosas, mitigar SQL injection/XSS, rate limiting, reglas gestionadas.
- **Precio:** `$5.00/Web ACL/mes` + `$1.00/1M` peticiones. Reglas gestionadas extra.

```bash
# Crear una Web ACL
aws wafv2 create-web-acl \
  --name mi-acl --scope CLOUDFRONT --default-action '{"Allow":{}}' \
  --visibility-config '{"SampledRequestsEnabled":true,"CloudWatchMetricsEnabled":true,"MetricName":"mi-acl"}' \
  --region us-east-1
aws wafv2 list-web-acls --scope CLOUDFRONT --region us-east-1

# IP Set (lista de IPs permitidas/bloqueadas)
aws wafv2 create-ip-set --name ips-malas --scope REGIONAL --ip-address-version IPV4 --addresses "1.2.3.4/32" --region us-east-1
aws wafv2 list-ip-sets --scope REGIONAL --region us-east-1

# Asociar la ACL a un recurso (ALB, CloudFront, API Gateway)
aws wafv2 associate-web-acl --web-acl-arn arn:aws:wafv2:... --resource-arn arn:aws:elasticloadbalancing:...

# Borrar
aws wafv2 delete-web-acl --name mi-acl --scope REGIONAL --id <Id> --lock-token <LockToken> --region us-east-1
```

---

## Gestión y gobierno

### CloudWatch

> **Monitorización y observabilidad** — métricas, logs y alarmas de todo lo que corre en AWS.

- **¿Qué es?** Recoge métricas (CPU, memoria, latencia...), logs y genera alarmas/notificaciones.
- **Casos de uso:** dashboards, alertas, monitorización de aplicaciones, auto scaling basado en métricas.
- **Precio:** métricas `$0.30/métrica/mes`; Logs `$0.50/GB` ingerido + `$0.03/GB` almacenado; alarmas `$0.10/alarma/mes`.

```bash
# Métricas
aws cloudwatch put-metric-data --namespace "MiApp" --metric-name "Pedidos" --value 42 --unit Count
aws cloudwatch list-metrics --namespace "MiApp"
aws cloudwatch get-metric-statistics --namespace "MiApp" --metric-name "Pedidos" --dimensions Name=Servicio,Value=Api --start-time 2026-07-01T00:00:00Z --end-time 2026-08-01T00:00:00Z --period 3600 --statistics Average

# Alarmas
aws cloudwatch put-metric-alarm \
  --alarm-name cpu-alta --alarm-description "CPU > 80%" \
  --metric-name CPUUtilization --namespace AWS/EC2 --statistic Average \
  --period 300 --threshold 80 --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2 --dimensions Name=InstanceId,Value=i-123 \
  --alarm-actions arn:aws:sns:us-east-1:123456789012:mi-topico
aws cloudwatch describe-alarms

# Logs
aws logs create-log-group --log-group-name /mi-app/prod
aws logs create-log-stream --log-group-name /mi-app/prod --log-stream-name api-1
aws logs put-log-events --log-group-name /mi-app/prod --log-stream-name api-1 --log-events '[{"timestamp":1750000000000,"message":"request ok"}]'
aws logs filter-log-events --log-group-name /mi-app/prod --filter-pattern "ERROR"
aws logs describe-log-groups
aws logs tail /mi-app/prod --follow
```

### CloudTrail

> **Auditoría de API** — registra cada llamada a la API de AWS (quién hizo qué y cuándo).

- **¿Qué es?** Registro inmutable de las acciones de los usuarios y servicios en tu cuenta (gobernanza, cumplimiento, forense).
- **Precio:** los **management events son gratis** (90 días de retención). Data events `$0.10/100k` eventos.

```bash
# Crear un trail (enviar a S3 / CloudWatch Logs)
aws cloudtrail create-trail --name mi-trail --s3-bucket-name mi-bucket-cloudtrail --is-multi-region-trail
aws cloudtrail start-logging --name mi-trail

# Buscar eventos
aws cloudtrail lookup-events --lookup-attributes AttributeKey=EventName,AttributeValue=TerminateInstances
aws cloudtrail lookup-events --lookup-attributes AttributeKey=Username,AttributeValue=ana --start-time 2026-07-01T00:00:00Z

# Estado
aws cloudtrail describe-trails
aws cloudtrail get-trail-status --name mi-trail
aws cloudtrail stop-logging --name mi-trail
```

### CloudFormation

> **Infraestructura como código (IaC)** — define toda tu infraestructura en plantillas YAML/JSON y desplegala.

- **¿Qué es?** Declaras recursos (EC2, S3, RDS...) en una plantilla y CloudFormation los crea/actualiza/borra de forma ordenada.
- **Casos de uso:** reproducir entornos, versionar infraestructura, despliegues repetibles.
- **Precio:** **gratis** (solo pagas los recursos que crea).

```bash
# Crear un stack desde una plantilla
aws cloudformation create-stack --stack-name mi-stack --template-body file://plantilla.yaml --capabilities CAPABILITY_NAMED_IAM
aws cloudformation create-stack --stack-name mi-stack --template-url https://s3.amazonaws.com/mi-bucket/plantilla.yaml

# Estado y eventos
aws cloudformation describe-stacks --stack-name mi-stack
aws cloudformation list-stacks --stack-status-filter CREATE_COMPLETE
aws cloudformation describe-stack-events --stack-name mi-stack

# Actualizar / borrar
aws cloudformation update-stack --stack-name mi-stack --template-body file://plantilla.yaml --capabilities CAPABILITY_NAMED_IAM
aws cloudformation delete-stack --stack-name mi-stack

# Validar una plantilla sin desplegar
aws cloudformation validate-template --template-body file://plantilla.yaml

# Change Sets (ver qué cambiará antes de aplicarlo)
aws cloudformation create-change-set --stack-name mi-stack --template-body file://plantilla.yaml --change-set-name cambios --capabilities CAPABILITY_NAMED_IAM
aws cloudformation execute-change-set --change-set-name cambios --stack-name mi-stack
```

### AWS Config

> **Cumplimiento y configuración** — audita continuamente la configuración de tus recursos.

- **¿Qué es?** Evalúa los recursos contra reglas (¿los buckets son públicos? ¿el cifrado está activo?) y genera un historial.
- **Precio:** `$0.003/item de configuración/mes` + reglas por evaluación.

```bash
aws configservice describe-configuration-recorders
aws configservice put-config-rule --config-rule '{"ConfigRuleName":"s3-publico","Source":{"Owner":"AWS","SourceIdentifier":"S3_BUCKET_PUBLIC_READ_PROHIBITED"}}'
aws configservice describe-config-rules
aws configservice describe-compliance-by-config-rule
aws configservice get-compliance-details-by-config-rule --config-rule-name s3-publico
```

### AWS Budgets

> **Presupuestos y alertas de coste** — no te lleves sorpresas en la factura.

- **¿Qué es?** Define presupuestos mensuales y alertas por email cuando el coste se acerca al límite.
- **Precio:** 2 presupuestos gratuitos; luego `$0.02/budget/día`.

```bash
aws budgets create-budget \
  --account-id 123456789012 \
  --budget '{"BudgetName":"Mi-presupuesto","BudgetLimit":{"Amount":"100","Unit":"USD"},"TimeUnit":"MONTHLY","BudgetType":"COST"}' \
  --notifications-with-subscribers '[{"Notification":{"NotificationType":"ACTUAL","ComparisonOperator":"GREATER_THAN","Threshold":80},"Subscribers":[{"SubscriptionType":"EMAIL","Address":"yo@midominio.com"}]}]'
aws budgets describe-budgets --account-id 123456789012
```

---

## Analítica

### Athena

> **SQL serverless sobre S3** — consulta archivos directamente sin cargarlos en una base de datos.

- **¿Qué es?** Ejecuta SQL directamente sobre datos en S3 (CSV, JSON, Parquet, Glue Catalog). Sin servidores.
- **Casos de uso:** análisis ad-hoc, consultas sobre logs, data lakes.
- **Precio:** `$5.00/TB` escaneado.

```bash
# Ejecutar una consulta
aws athena start-query-execution \
  --query-string "SELECT nombre, COUNT(*) FROM \"mi-bucket\".\"mi-tabla\" GROUP BY nombre" \
  --result-configuration '{"OutputLocation":"s3://athena-results/"}'
aws athena get-query-execution --query-execution-id <QueryExecutionId>
aws athena get-query-results --query-execution-id <QueryExecutionId>

# Workgroups (organización y control de costes)
aws athena create-work-group --name analitica --description "Workgroup analitica"
aws athena list-work-groups
```

### AWS Glue

> **ETL serverless** — prepara y transforma datos para analítica.

- **¿Qué es?** Servicio de integración de datos: crawlers que catalogan datos en S3, y jobs ETL (Python/Scala/Spark).
- **Casos de uso:** construir data lakes, limpiar/transformar datos, catálogo de tablas para Athena.
- **Precio:** `$0.44/DPU-hora` (jobs) — el catálogo y los crawlers tienen su propia tarifa.

```bash
# Catálogo: bases de datos y tablas
aws glue create-database --database-input '{"Name":"mi-db"}'
aws glue get-databases
aws glue get-tables --database-name mi-db

# Crawlers (descubren esquemas automáticamente)
aws glue create-crawler --name mi-crawler --role arn:aws:iam::123456789012:role/glue-role --targets '{"S3Targets":[{"Path":"s3://mi-bucket/datos"}]}' --database-name mi-db
aws glue start-crawler --name mi-crawler
aws glue get-crawler --name mi-crawler

# Jobs ETL
aws glue create-job --name mi-job --role arn:aws:iam::123456789012:role/glue-role --command '{"Name":"pythonshell","PythonVersion":"3","ScriptLocation":"s3://mi-bucket/scripts/etl.py"}' --max-capacity 2
aws glue start-job-run --job-name mi-job
aws glue get-job-runs --job-name mi-job
```

### Amazon EMR

> **Big data** — clústeres gestionados de Hadoop, Spark, Hive, Presto.

- **¿Qué es?** Lanza clústeres de cómputo distribuido para procesar grandes volúmenes de datos.
- **Casos de uso:** procesamiento Spark masivo, ETL de big data, machine learning distribuido.
- **Precio:** EC2 subyacente + recargo por nodo (~30%).

```bash
# Crear un clúster
aws emr create-cluster \
  --name mi-cluster \
  --release-label emr-7.1.0 \
  --applications Name=Spark \
  --instance-groups '[{"InstanceCount":2,"InstanceGroupType":"CORE","InstanceType":"m5.xlarge"}]' \
  --ec2-attributes '{"KeyName":"mi-clave","InstanceProfile":"EMR_EC2_DefaultRole"}' \
  --service-role EMR_DefaultRole
aws emr list-clusters

# Añadir pasos (Spark jobs)
aws emr add-steps --cluster-id j-123 --steps '[{"Name":"mi-paso","Jar":"command-runner.jar","Args":["spark-submit","s3://mi-bucket/scripts/job.py"],"ActionOnFailure":"CONTINUE"}]'

# Estado y terminación
aws emr describe-cluster --cluster-id j-123
aws emr terminate-job-flows --cluster-ids j-123
```

---

## Machine Learning e IA

### Amazon SageMaker

> **Plataforma completa de ML** — construye, entrena y despliega modelos de machine learning.

- **¿Qué es?** Entorno gestionado para todo el ciclo de vida de ML: notebooks, entrenamiento distribuido, despliegue de endpoints.
- **Casos de uso:** modelos de predicción, clasificación, recomendación, NLP y visión.
- **Precio:** desde ~`$0.05/h` (notebook `ml.t3.medium`) hasta cientos de $/h para GPUs de entrenamiento.

```bash
# Notebook instance
aws sagemaker create-notebook-instance --notebook-instance-name mi-notebook --instance-type ml.t3.medium --role-arn arn:aws:iam::123456789012:role/sagemaker-role

# Entrenamiento
aws sagemaker create-training-job --training-job-name mi-entreno --algorithm-specification '{"TrainingImage":"...","TrainingInputMode":"File"}' --role-arn arn:aws:iam::123456789012:role/sagemaker-role --resource-config '{"InstanceType":"ml.m5.large","InstanceCount":1}'
aws sagemaker list-training-jobs

# Desplegar un modelo
aws sagemaker create-model --model-name mi-modelo --primary-container '{"Image":"...","ModelDataUrl":"s3://mi-bucket/model/model.tar.gz"}'
aws sagemaker create-endpoint-config --endpoint-config-name mi-config --production-variants '[{"VariantName":"v1","ModelName":"mi-modelo","InitialInstanceCount":1,"InstanceType":"ml.m5.large"}]'
aws sagemaker create-endpoint --endpoint-name mi-endpoint --endpoint-config-name mi-config
aws sagemaker describe-endpoint --endpoint-name mi-endpoint
```

### Amazon Bedrock

> **Modelos de IA generativa como API** — accede a Claude, Llama, Mistral, Titan, Nova, etc. sin gestionar infraestructura.

- **¿Qué es?** API única para usar modelos fundacionales (FMs) de diferentes proveedores (Anthropic, Meta, Mistral, AI21, Cohere, Amazon).
- **Casos de uso:** chatbots, generación de texto, resúmenes, agentes, RAG.
- **Precio:** por tokens/uso de cada modelo (varía por modelo; algunos tienen free tier).

```bash
# Listar modelos fundacionales disponibles
aws bedrock list-foundation-models --output table

# Invocar un modelo (Claude)
aws bedrock-runtime invoke-model \
  --model-id anthropic.claude-3-5-sonnet-20241022-v2:0 \
  --body '{"anthropic_version":"bedrock-2023-05-31","max_tokens":512,"messages":[{"role":"user","content":"Explica AWS en una frase"}]}' \
  --region us-east-1 out.json

# Invocar con streaming (respuesta en tiempo real)
aws bedrock-runtime invoke-model-with-response-stream --model-id anthropic.claude-3-5-sonnet-... --body '{"anthropic_version":"bedrock-2023-05-31","max_tokens":128,"messages":[{"role":"user","content":"Hola"}]}'

# API Converse (interfaz unificada para todos los modelos)
aws bedrock-runtime converse \
  --model-id amazon.nova-micro-v1:0 \
  --messages '[{"role":"user","content":[{"text":"¿Cuánto es 2+2?"}]}]'
```

---

## Herramientas de desarrollo y DevOps

### CodeCommit / CodeBuild / CodePipeline / CodeDeploy

> **Suite CI/CD nativa de AWS** — repositorios git, builds, pipelines y despliegues.

| Servicio | Función | Precio |
|----------|---------|--------|
| **CodeCommit** | Repositorios Git privados | Gratis (5 usuarios, 50 GB) |
| **CodeBuild** | Builds serverless en contenedores | Por minuto de build (`$0.005/min` Linux) |
| **CodePipeline** | Orquestación CI/CD (source → build → deploy) | `$1.00/pipeline/mes` (activo) |
| **CodeDeploy** | Despliegues automatizados (EC2, ECS, Lambda, on-premise) | Gratis |

```bash
# CodeCommit
aws codecommit create-repository --repository-name mi-repo
aws codecommit list-repositories
git push https://git-codecommit.us-east-1.amazonaws.com/v1/repos/mi-repo

# CodeBuild
aws codebuild create-project --name mi-build --source '{"Type":"CODECOMMIT","Location":"..."}' --environment '{"Type":"LINUX_CONTAINER","ComputeType":"BUILD_GENERAL1_SMALL","Image":"aws/codebuild/amazonlinux2-x86_64-standard:5.0"}' --service-role arn:aws:iam::123456789012:role/codebuild-role
aws codebuild start-build --project-name mi-build
aws codebuild batch-get-builds --ids <BuildId>

# CodePipeline
aws codepipeline create-pipeline --cli-input-json file://pipeline.json
aws codepipeline list-pipelines
aws codepipeline start-pipeline-execution --name mi-pipeline

# CodeDeploy
aws deploy create-application --application-name mi-app
aws deploy create-deployment --application-name mi-app --deployment-group-name mi-grupo --s3-location bucket=mi-bucket,key=app.zip,bundleType=zip
```

---

## Servicios usados en este proyecto

Este proyecto educativo usa **uno** de los más de 200 servicios de AWS:

| Servicio AWS | Uso en el proyecto | Alternativa local |
|--------------|--------------------|-------------------|
| **SSM Parameter Store** | Configuración centralizada (connection strings, message broker, OpenTelemetry) de la API, OutboxProcessor y Consumer | **MiniStack** (emulador en `localhost:4566`) |

- **SSM Parameter Store** se usa para leer la configuración en tiempo de ejecución, sobreescribiendo los `appsettings.json` (que quedan como fallback).
- La integración se implementa en .NET con `Amazon.Extensions.Configuration.SystemsManager`.
- En **desarrollo local** los parámetros vienen de MiniStack; en **producción** de AWS real (IAM Role + `SecureString`).

Para los detalles de implementación y cómo emular AWS en local:

| Tema | Documento |
|------|-----------|
| **MiniStack (emulador local)** | [`README.Ministack.md`](README.Ministack.md) |
| **Integración SSM en .NET** | [`README.Ministack.md`](README.Ministack.md) |
| **README principal del proyecto** | [`README.md`](README.md) |

---

## Referencias

- [AWS — Página oficial](https://aws.amazon.com/)
- [AWS Cloud Computing](https://aws.amazon.com/what-is-aws/)
- [AWS Pricing](https://aws.amazon.com/pricing/)
- [AWS Pricing Calculator](https://calculator.aws/)
- [AWS Free Tier](https://aws.amazon.com/free/)
- [AWS CLI Command Reference](https://awscli.amazonaws.com/v2/documentation/api/latest/index.html)
- [AWS Services — Full list](https://aws.amazon.com/products/)
- [AWS Global Infrastructure](https://aws.amazon.com/about-aws/global-infrastructure/)
- [Documentación de SSM Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html)
- [IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html)
- [AWS Architecture Center](https://aws.amazon.com/architecture/)

---

**Feliz aprendizaje!** 🚀
