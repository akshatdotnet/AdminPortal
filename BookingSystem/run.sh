#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# BookingSystem - Local Setup & Run Script
# Usage: chmod +x run.sh && ./run.sh
# ─────────────────────────────────────────────────────────────────────────────

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}"
echo "╔══════════════════════════════════════════════╗"
echo "║       Booking System - .NET 8 + EF Core      ║"
echo "║   Booking → Order → Payment → Notification   ║"
echo "╚══════════════════════════════════════════════╝"
echo -e "${NC}"

# Check dotnet
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK not found. Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VER=$(dotnet --version)
echo -e "${GREEN}✅ .NET SDK: $DOTNET_VER${NC}"

# Restore
echo -e "\n${YELLOW}📦 Restoring packages...${NC}"
cd "$(dirname "$0")"
dotnet restore BookingSystem.sln

# Build
echo -e "\n${YELLOW}🔨 Building solution...${NC}"
dotnet build BookingSystem.sln -c Debug --no-restore

# EF Migrations
echo -e "\n${YELLOW}🗄️  Setting up database (SQLite)...${NC}"
cd src/BookingSystem.API
dotnet ef database update 2>/dev/null || echo "Note: run 'dotnet ef migrations add Init' if first time"
cd ../..

# Run API
echo -e "\n${GREEN}🚀 Starting Booking System API...${NC}"
echo -e "${BLUE}   Swagger UI  → http://localhost:5000${NC}"
echo -e "${BLUE}   Health      → http://localhost:5000/health${NC}"
echo -e "${BLUE}   Events log  → http://localhost:5000/api/events${NC}"
echo -e "${BLUE}   Full demo   → POST http://localhost:5000/api/demo/full-flow${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop${NC}\n"

cd src/BookingSystem.API
dotnet run --no-build
