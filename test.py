import socket

client = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

message = "Hello UDP Server!"
client.sendto(message.encode(), ("127.0.0.1", 9000))

data, server = client.recvfrom(1024)
print(f"📩 Nhận từ server: {data.decode()}")
