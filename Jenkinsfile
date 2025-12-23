pipeline {
    agent any

    environment {
        DOCKER_IMAGE = "hoangnhat2003/drinkshop-api"
        DOCKER_HUB_CREDS = 'docker-hub-credentials-id'
    }

    stages {
        stage('Clone Code') {
            steps {
                checkout scm
            }
        }

        stage('Build & Push Docker Image') {
            steps {
                script {
                    docker.withRegistry('https://index.docker.io/v1/', DOCKER_HUB_CREDS) {
                        def customImage = docker.build("${DOCKER_IMAGE}:${BUILD_NUMBER}")
                        customImage.push()
                        customImage.push("latest")
                    }
                }
            }
        }

        stage('Deploy to Development') {
            steps {
                script {
                    withCredentials([
                        string(credentialsId: 'drinkshop-db-password', variable: 'DB_PWD'),
                        string(credentialsId: 'drinkshop-minio-user', variable: 'MINIO_USER'),
                        string(credentialsId: 'drinkshop-minio-pass', variable: 'MINIO_PWD'),
                        string(credentialsId: 'drinkshop-jwt-secret', variable: 'JWT_SEC')
                    ]) {
                        // Tạo chuỗi kết nối động cho .NET
                        def connStr = "Server=db;Database=DrinkShopDb;User Id=sa;Password=${DB_PWD};TrustServerCertificate=True;"
                        
                        sh """
                            IMAGE_TAG=${BUILD_NUMBER} \
                            DB_PASSWORD=${DB_PWD} \
                            MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} \
                            JWT_SECRET=${JWT_SEC} \
                            MINIO_ENDPOINT=minio:9000 \
                            MINIO_BUCKET=drinkshop-bucket-prod \
                            MINIO_USE_SSL=false \
                            CONNECTION_STRING="${connStr}" \
                            docker-compose -f docker-compose.prod.yml up -d --build
                        """
                    }
                }
            }
        }

        stage('Cleanup') {
            steps {
                sh 'docker image prune -f'
            }
        }
    }
}