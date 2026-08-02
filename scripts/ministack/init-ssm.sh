#!/bin/sh
set -e

# Evita que AWS CLI intente descargar las URLs que empiezan por http://
export AWS_CLI_FILE_ENCODING=utf-8

echo "Seeding SSM parameters for SoftwareLearningGuide..."

# --- API ---
aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/ConnectionStrings/SoftwareLearningGuide" \
  --value "Server=localhost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/OpenTelemetry/ServiceName" \
  --value "SoftwareLearningGuide" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/OpenTelemetry/Otlp/Endpoint" \
  --cli-input-json '{"Name": "/softwarelearningguide/dev/api/OpenTelemetry/Otlp/Endpoint", "Value": "http://localhost:4317", "Type": "String", "Overwrite": true}'

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/OpenTelemetry/Otlp/Protocol" \
  --value "grpc" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/Enabled" \
  --value "true" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/Region" \
  --value "us-east-1" \
  --type String \
  --overwrite

# UserPoolId y ClientId los genera cognito-init.sh (son dinámicos en MiniStack)
# NOTA: el valor empieza por "http://" y AWS CLI v1 lo interpreta como una URL a descargar,
# guardando el contenido (el XML de ListBuckets de MiniStack) en vez de la URL. Por eso,
# igual que los demás valores "http://", se pasa con --cli-input-json para evitar la descarga.
aws ssm put-parameter \
  --cli-input-json '{"Name": "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/ServiceUrl", "Value": "http://localhost:4566", "Type": "String", "Overwrite": true}'

# --- OutboxProcessor ---
aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/Database/SoftwareLearningGuide" \
  --value "Server=localhost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/MessageBroker/Host" \
  --value "localhost" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/MessageBroker/Username" \
  --value "guest" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/MessageBroker/Password" \
  --value "guest" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/OpenTelemetry/ServiceName" \
  --value "SoftwareLearningGuide.OutboxProcessor" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/OpenTelemetry/Otlp/Endpoint" \
  --cli-input-json '{"Name": "/softwarelearningguide/dev/outboxprocessor/OpenTelemetry/Otlp/Endpoint", "Value": "http://localhost:4317", "Type": "String", "Overwrite": true}'

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/OpenTelemetry/Otlp/Protocol" \
  --value "grpc" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/CloudProvidersConfigurations/AWS/Messaging/Enabled" \
  --value "true" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/CloudProvidersConfigurations/AWS/Messaging/Region" \
  --value "us-east-1" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/outboxprocessor/CloudProvidersConfigurations/AWS/Messaging/ServiceUrl" \
  --cli-input-json '{"Name": "/softwarelearningguide/dev/outboxprocessor/CloudProvidersConfigurations/AWS/Messaging/ServiceUrl", "Value": "http://localhost:4566", "Type": "String", "Overwrite": true}'

# --- Consumer ---
aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/MessageBroker/Host" \
  --value "localhost" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/MessageBroker/Username" \
  --value "guest" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/MessageBroker/Password" \
  --value "guest" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/OpenTelemetry/ServiceName" \
  --value "SoftwareLearningGuide.Consumer" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/OpenTelemetry/Otlp/Endpoint" \
  --cli-input-json '{"Name": "/softwarelearningguide/dev/consumer/OpenTelemetry/Otlp/Endpoint", "Value": "http://localhost:4317", "Type": "String", "Overwrite": true}'

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/OpenTelemetry/Otlp/Protocol" \
  --value "grpc" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/CloudProvidersConfigurations/AWS/Messaging/Enabled" \
  --value "true" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/CloudProvidersConfigurations/AWS/Messaging/Region" \
  --value "us-east-1" \
  --type String \
  --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/consumer/CloudProvidersConfigurations/AWS/Messaging/ServiceUrl" \
  --cli-input-json '{"Name": "/softwarelearningguide/dev/consumer/CloudProvidersConfigurations/AWS/Messaging/ServiceUrl", "Value": "http://localhost:4566", "Type": "String", "Overwrite": true}'

echo "SSM parameters seeded successfully."