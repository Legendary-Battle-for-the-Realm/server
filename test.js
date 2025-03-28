const dgram = require("dgram");
const server = dgram.createSocket("udp4");

server.on("message", (msg, rinfo) => {
  console.log(`📩 Nhận từ ${rinfo.address}:${rinfo.port}: ${msg.toString()}`);

  // Gửi phản hồi lại client
  const response = Buffer.from("✅ Nhận được gói tin UDP!");
  server.send(response, rinfo.port, rinfo.address, (err) => {
    if (err) console.error("Lỗi khi gửi phản hồi:", err);
  });
});

server.bind(9000, "0.0.0.0", () => {
  console.log("🚀 UDP Server đang chạy trên cổng 9000...");
});
