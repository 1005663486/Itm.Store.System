variable "dockerhub_username" {
  type        = string
  description = "Usuario de Docker Hub"
  default     = "mariacamila2404"
}

variable "image_tag" {
  type        = string
  description = "Tag de las imágenes a desplegar"
  default     = "latest"
}