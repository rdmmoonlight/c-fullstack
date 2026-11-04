#include <iostream>
#include <cstring>
#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>

int main() {
    int server_fd = socket(AF_INET, SOCK_STREAM, 0);
    sockaddr_in address{};
    address.sin_family = AF_INET;
    address.sin_addr.s_addr = INADDR_ANY;
    address.sin_port = htons(8080);

    bind(server_fd, (struct sockaddr*)&address, sizeof(address));
    listen(server_fd, 10);

    std::cout << "Backend C++ listening on port 8080..." << std::endl;

    while (true) {
        int new_socket = accept(server_fd, nullptr, nullptr);
        const char* response = 
            "HTTP/1.1 200 OK\r\n"
            "Content-Type: application/json\r\n"
            "Access-Control-Allow-Origin: *\r\n\r\n"
            "{\"message\": \"Hello from C++ Backend!\", \"status\": \"success\"}";
        
        send(new_socket, response, strlen(response), 0);
        close(new_socket);
    }
    return 0;
}
