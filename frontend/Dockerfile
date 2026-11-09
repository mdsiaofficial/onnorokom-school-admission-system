FROM node:26-alpine
WORKDIR /app

# Copy dependency specifications
COPY package.json package-lock.json* ./

# Install npm dependencies
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

# Copy frontend application code
COPY . .

EXPOSE 3000

ENV PORT=3000 \
    HOST=0.0.0.0

CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]
