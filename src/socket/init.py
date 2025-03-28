import socket
import asyncio
import time
from .req_handler import handler

def gen_text(msg: str):
    return f"""HTTP/1.1 200 Ok
Content-Type: text/plain

{msg}
""".encode()

class UDPServerProtocol:
    def connection_made(self, transport):
        self.transport = transport
        print("UDP Server đang chạy trên 127.0.0.1:9000\n==========================================\n\n")

    def datagram_received(self, data, addr):
        try:
            decoded_data = data.decode("utf-8")  # Decode với UTF-8
        except UnicodeDecodeError:
            decoded_data = data.decode("latin-1", errors="replace")  # Thử với Latin-1

        print(f"Nhận từ {addr}: {decoded_data}")

        response = f"Server nhận được: {decoded_data}"
        self.transport.sendto(response.encode(), addr)

async def main():
    loop = asyncio.get_running_loop()
    print("Khởi động UDP Server...")
    
    # Tạo UDP server
    listen = await loop.create_datagram_endpoint(
        lambda: UDPServerProtocol(),
        local_addr=("127.0.0.1", 9000),
    )

    # Giữ server chạy
    transport, protocol = listen
    try:
        await asyncio.Future()
    finally:
        transport.close()

def udp_server():
    asyncio.run(main())