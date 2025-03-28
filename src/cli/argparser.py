import argparse

def handle_args():
    parser = argparse.ArgumentParser(description="Chương trình CLI")
    parser.add_argument("--pem", action="store_true", help="Tạo folder PEM")

    args = parser.parse_args()

    if args.pem:
        from src.service.pem_gen import gen
        gen()