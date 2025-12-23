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
                    // PHẢI thêm đầy đủ các bí mật bạn đã tạo trong Jenkins vào đây
                    withCredentials([
                        string(credentialsId: 'drinkshop-db-password', variable: 'DB_PWD'),
                        string(credentialsId: 'drinkshop-minio-user', variable: 'MINIO_USER'),
                        string(credentialsId: 'drinkshop-minio-pass', variable: 'MINIO_PWD'),
                        string(credentialsId: 'drinkshop-jwt-secret', variable: 'JWT_SEC')
                    ]) {
                        // Tạo chuỗi kết nối động dựa trên mật khẩu vừa lấy
                        def connStr = "Server=db;Database=DrinkShopDb;User Id=sa;Password=${DB_PWD};TrustServerCertificate=True;"
                        
                        sh """
                            IMAGE_TAG=${BUILD_NUMBER} \
                            DB_PASSWORD=${DB_PWD} \
                            MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} \
                            JWT_SECRET=${JWT_SEC} \
                            CONNECTION_STRING="${connStr}" \
                            docker-compose up -d --build
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