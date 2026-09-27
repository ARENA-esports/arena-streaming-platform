# ==========================================
# Stage 1: Build the React Application
# ==========================================
FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

ARG VITE_USER_API_URL
ARG VITE_TOURNAMENT_API_URL
ARG VITE_STREAM_API_URL

ENV VITE_USER_API_URL=$VITE_USER_API_URL
ENV VITE_TOURNAMENT_API_URL=$VITE_TOURNAMENT_API_URL
ENV VITE_STREAM_API_URL=$VITE_STREAM_API_URL

RUN npm run build

# ==========================================
# Stage 2: Serve the static files with Nginx
# ==========================================

FROM nginx:alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]