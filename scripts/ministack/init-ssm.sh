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

echo "SSM parameters seeded successfully."