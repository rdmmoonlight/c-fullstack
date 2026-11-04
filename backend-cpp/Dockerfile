# Build Stage
FROM alpine:latest AS builder
RUN apk add --no-cache g++ cmake make
WORKDIR /app
COPY . .
RUN cmake . && make

# Run Stage
FROM alpine:latest
RUN apk add --no-cache libstdc++
WORKDIR /app
COPY --from=builder /app/cpp_backend .
EXPOSE 8080
CMD ["./cpp_backend"]
