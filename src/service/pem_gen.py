import os
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives import hashes
from cryptography.x509 import NameOID
import cryptography.x509 as x509
from datetime import datetime, timedelta

def gen():
    pem_dir = "pem"
    os.makedirs("pem", exist_ok=True)

    # **1️⃣ Tạo khóa RSA riêng tư (private key)**
    private_key = rsa.generate_private_key(
        public_exponent=65537,
        key_size=2048,
    )

    # Lưu private key vào file "pem/key.pem"
    private_key_path = os.path.join(pem_dir, "key.pem")
    with open(private_key_path, "wb") as f:
        f.write(
            private_key.private_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PrivateFormat.TraditionalOpenSSL,
                encryption_algorithm=serialization.NoEncryption(),
            )
        )
    print(f"🔑 Đã tạo: {private_key_path}")

    # **2️⃣ Tạo chứng chỉ (certificate)**
    subject = issuer = x509.Name([
        x509.NameAttribute(NameOID.COUNTRY_NAME, "VN"),
        x509.NameAttribute(NameOID.STATE_OR_PROVINCE_NAME, "Ho Chi Minh"),
        x509.NameAttribute(NameOID.LOCALITY_NAME, "Ho Chi Minh"),
        x509.NameAttribute(NameOID.ORGANIZATION_NAME, "My App"),
        x509.NameAttribute(NameOID.COMMON_NAME, "App"),
    ])

    cert = x509.CertificateBuilder().subject_name(subject)\
        .issuer_name(issuer)\
        .public_key(private_key.public_key())\
        .serial_number(x509.random_serial_number())\
        .not_valid_before(datetime.utcnow())\
        .not_valid_after(datetime.utcnow() + timedelta(days=365))\
        .add_extension(x509.BasicConstraints(ca=True, path_length=None), critical=True)\
        .sign(private_key, hashes.SHA256())

    # Lưu certificate vào file "pem/cert.pem"
    cert_path = os.path.join(pem_dir, "cert.pem")
    with open(cert_path, "wb") as f:
        f.write(cert.public_bytes(serialization.Encoding.PEM))

    print(f"📜 Đã tạo: {cert_path}")
