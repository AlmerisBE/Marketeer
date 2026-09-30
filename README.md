# Marketeer

![Dalamud API](https://img.shields.io/badge/Dalamud%20API-v10-blue.svg)
![License](https://img.shields.io/badge/License-AGPL%203.0-green.svg)
![Language](https://img.shields.io/badge/Language-C%23%2012.0-purple.svg)

**Marketeer** is an all-in-one market board, inventory, and retainer management plugin for Final Fantasy XIV (via Dalamud). It is designed to optimize your trading strategies, eliminate tedious market board clicking, and maximize your profits across all your characters.

## 🌟 Key Features

### ⚔️ Sales & Competition
* **Undercut Tracking:** Instantly identify which of your listings have been undercut and match server-lowest prices.
* **Loss Prevention & Price Alerts:** Get warned before selling items below their NPC vendor buy price.
* **Market Watchlist:** Set target buy/sell prices for specific items. Marketeer will silently monitor the market and notify you in the chat when an opportunity arises.

### 🤖 Retainer Automation & Guidance
* **Stateless Event-Driven Automation:** Experience lightning-fast, zero-delay retainer interactions.
* **Smart Guidance Window:** A context-aware overlay that appears when you interact with a Summoning Bell, offering 1-click execution for price adjustments, listing cancellations, and retainer switching.
* **Auto-Switcher:** Seamlessly transitions between retainers natively without manual menu navigation.

### 📊 Analysis & Financial History
* **Sales History:** A dedicated tracker for your completed sales, powered by a highly optimized, asynchronous SQLite storage layer.
* **Dynamic Charts & Metrics:** Visualize your revenue over the last 14 days, identify your top 10 best-sellers, and track item sales velocity (sales per day).
* **Multi-Character Dashboard:** Track liquid Gil and active market values across all your alts and retainers without needing to log into them.

### 🔨 Crafting Profit Evaluator
* **Recursive Material Requirements Planning (MRP):** Predictively calculate the profitability of your crafting sessions.
* **Inventory Awareness:** The evaluator automatically scans your aggregated cross-character and retainer inventories to factor in the materials you already own, calculating exact net profits based on live Universalis pricing and historical velocity.

## 📥 Installation

1. Open the Dalamud Settings menu in FFXIV (`/xlsettings`).
2. Go to the **Experimental** tab.
3. Under **Custom Plugin Repositories**, add the following URL:
   ```text
   [https://raw.githubusercontent.com/AlmerisBE/Marketeer/main/repo.json](https://raw.githubusercontent.com/AlmerisBE/Marketeer/main/repo.json)
