def handler(data):
    print(f"nhận dữ liệu: {data.decode()}")

    data = data.decode().split("\r\n\r\n")[1]

    return data