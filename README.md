# 📧 Bulk Email Sender — Gmail SMTP + Google Sheets (By Rayri)

Send personalized, designed bulk emails to a list of recipients pulled automatically from a Google Sheet. Built with C# (.NET 8), Gmail SMTP, and the Google Sheets API.

---

## 📋 Table of Contents

- [How It Works](#how-it-works)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Google Sheets Format](#google-sheets-format)
- [Setup Guide](#setup-guide)
  - [Step 1 — Clone the Repository](#step-1--clone-the-repository)
  - [Step 2 — Get Your Gmail App Password](#step-2--get-your-gmail-app-password)
  - [Step 3 — Set Up Google Sheets API](#step-3--set-up-google-sheets-api)
  - [Step 4 — Upload Images to GitHub](#step-4--upload-images-to-github)
  - [Step 5 — Configure Your Credentials](#step-5--configure-your-credentials)
  - [Step 6 — Set Environment in Visual Studio](#step-6--set-environment-in-visual-studio)
  - [Step 7 — Run the App](#step-7--run-the-app)
- [Previewing Your Email](#previewing-your-email)
- [Customizing the Email Template](#customizing-the-email-template)
- [Security Notes](#security-notes)
- [Troubleshooting](#troubleshooting)

---

## How It Works

```
Google Sheet (recipient list)
        │
        ▼
  C# App reads Column A (email) + Column B (name)
              + Column C (position) + Column D (status)
        │
        ├─ Column D = "Sent"? → ⏭️  Skip recipient
        │
        ▼
  Column C = "Member"?
        ├── Yes → Load email_template.html
        └── No  → Load email_other.html
        │
        ▼
  Replace {{name}}, {{email}}, {{position}}
  with each recipient's actual data
        │
        ▼
  Send via Gmail SMTP (1 email every 1.5 seconds)
        │
        ▼
  Console logs ✅ Sent, ⏭️ Skipped, or ❌ Failed per recipient
```
---


## Prerequisites

Before you start, make sure you have:

- [.NET 8 SDK](https://dotnet.microsoft.com/download) installed
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community edition is free)
- A **Gmail account** with 2-Step Verification enabled
- A **Google account** to access Google Cloud Console (free)
- A **GitHub account** (free) to host your images

---

## Project Structure

```
BulkEmailSender/
├── .gitignore
├── .env.example
├── README.md
├── assets/
│   └── images/
│       ├── logo.png          ← hosted here for GitHub raw URLs
│       ├── banner.jpg
│       ├── icon.png
│       └── facebook.png
│
├── BulkEmailSender.sln
└── BulkEmailSender/
    ├── BulkEmailSender.csproj
    ├── appsettings.json                   ← committed (empty structure)
    ├── appsettings.Development.json       ← NOT committed (your real values)
    ├── credentials.json                   ← NOT committed (Google key)
    ├── Config/
    │   └── AppSettings.cs
    ├── Services/
    │   ├── GmailSmtpService.cs
    │   └── GoogleSheetsService.cs
    ├── Models/
    │   └── Recipient.cs
    ├── Templates/
    │   ├── assets/
    │   │   └── images/                    ← local copies for browser preview only
    │   ├── email_template.html            ← what gets sent with only name (GitHub raw URLs)
    │   ├── email_other.html               ← what gets sent with both name and position
    │   
    └── Program.cs
```

---

## Google Sheets Format

Your Google Sheet must follow this exact column layout:

| A | B | C | D |
|---|---|---|---|
| **Email** | **Name** | **Position** | **Status** |
| juan@example.com | Juan dela Cruz | Member | |
| maria@example.com | Maria Santos | President | |
| pedro@example.com | Pedro Reyes | Member | Sent |

**Rules:**
- **Row 1** must be a header row — it is automatically skipped by the app
- **Column A** = email address (required)
- **Column B** = recipient's name — replaces `{{name}}` in both templates
- **Column C** = position — determines which template is sent:
  - `Member` → sends `email_template.html`
  - Anything else (e.g. `President`, `Secretary`, `Officer`) → sends `email_other.html`
  - The check is **case-insensitive** — `member`, `MEMBER`, and `Member` all match
- **Column D** = send status — if the cell contains `Sent` (case-insensitive), that row is **skipped entirely**. Leave blank to send normally.
- The sheet tab must be named **Sheet1** (the default when creating a new sheet)
- No blank rows between entries
- No extra spaces before or after email addresses

**Tip:** After a successful send, manually type `Sent` in Column D for that row. This prevents accidental re-sends if you run the app again.

---

Your Google Sheet must follow this exact column layout:

| A | B | C | D |
|---|---|---|---|
| **Email** | **Name** |**Position** | **Status** |
| juan@example.com | Juan dela Cruz | Member | Sent |
| maria@example.com | Maria Santos |
| pedro@example.com | Pedro Reyes |

**Rules:**
- **Row 1** must be a header row — it is automatically skipped by the app
- **Column A** = email address (required)
- **Column B** = recipient's name (replaces `{{name}}` in the email body)
- The sheet tab must be named **Sheet1** (the default when creating a new sheet)
- No blank rows between entries
- No extra spaces before or after email addresses

**How to find your Sheet ID:**

Open your Google Sheet and look at the URL:
```
https://docs.google.com/spreadsheets/d/THIS_IS_YOUR_SHEET_ID/edit
```
Copy the long string between `/d/` and `/edit` — that is your Sheet ID.

---

## Setup Guide

### Step 1 — Clone the Repository

In Visual Studio: **File → Clone Repository** → paste the GitHub URL.

Or via terminal:
```bash
git clone https://github.com/YOURUSERNAME/BulkEmailSenderUsingMailtrapandGoogleSheets.git
cd BulkEmailSenderUsingMailtrapandGoogleSheets
```

---

### Step 2 — Get Your Gmail App Password

> You must use a personal Gmail account (`@gmail.com`).
> School or work Google Workspace accounts may have App Passwords disabled by the admin.

1. Go to [myaccount.google.com](https://myaccount.google.com)
2. Click **Security** in the left sidebar
3. Under **"How you sign in to Google"**, enable **2-Step Verification** if not already on
4. Once 2-Step Verification is on, go directly to:
   **[myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)**
5. In the name field type `BulkEmailSender` → click **Create**
6. Google shows a **16-character password** — copy it immediately (shown only once)
7. Keep it safe — you will paste it into your config in Step 5

---

### Step 3 — Set Up Google Sheets API

**3a. Create a Google Cloud project**
1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Click the project dropdown at the top → **New Project**
3. Name it `BulkEmailSender` → click **Create**
4. Make sure the new project is selected in the dropdown

**3b. Enable the Sheets API**
1. In the top search bar type `Google Sheets API` → click it
2. Click **Enable**

**3c. Create a Service Account**
1. In the left sidebar go to **APIs & Services** → **Credentials**
2. Click **+ Create Credentials** → **Service Account**
3. Name it `bulk-email-reader` → click **Create and Continue**
4. Skip the optional steps → click **Done**

**3d. Download credentials.json**
1. Click on your new service account in the list
2. Go to the **Keys** tab
3. Click **Add Key** → **Create New Key** → select **JSON** → click **Create**
4. A file downloads automatically — rename it to `credentials.json`
5. Move it into the project folder at this location:
   ```
   BulkEmailSender/
       └── BulkEmailSender/
               ├── appsettings.json
               ├── credentials.json    ← place it here
   ```

**3e. Share your Google Sheet with the service account**
1. Open `credentials.json` in Notepad or any text editor
2. Find the `"client_email"` field — it looks like:
   ```
   bulk-email-reader@your-project-id.iam.gserviceaccount.com
   ```
3. Open your Google Sheet → click **Share** (top right)
4. Paste that email address → set permission to **Viewer** → click **Send**

---

### Step 4 — Upload Images to GitHub

This makes your images accessible via a public URL so email clients can load them.

1. In your GitHub repo, go to the `assets/images/` folder
2. Upload your image files and name them exactly:
   - `banner-top.jpg`   → top banner (600px wide)
   - `banner-bottom.jpg` → bottom footer banner (600px wide)
   - `icon-1.png`       → first small icon (35px wide)
   - `icon-2.png`       → second small icon (35px wide)
3. After uploading, click each image in GitHub → click the **Raw** button
4. Copy the URL from your browser address bar. It looks like:
```
   https://raw.githubusercontent.com/YOURUSERNAME/YOURREPO/main/assets/images/banner-top.jpg
```
5. Open `Templates/email_template.html` and replace the four placeholders with your copied URLs:
   - `{{GITHUB_RAW_URL_BANNER_TOP}}`    → your top banner raw URL
   - `{{GITHUB_RAW_URL_BANNER_BOTTOM}}` → your bottom banner raw URL
   - `{{GITHUB_RAW_URL_ICON_1}}`        → your first icon raw URL
   - `{{GITHUB_RAW_URL_ICON_2}}`        → your second icon raw URL

---

### Step 5 — Configure Your Credentials

Open `appsettings.Development.json` inside the project folder and fill in your values:

```json
{
  "Gmail": {
    "SenderEmail": "yourgmailaddress@gmail.com",
    "SenderName": "Your Name or Organization",
    "AppPassword": "abcdabcdabcdabcd"
  },
  "Google": {
    "SheetId": "paste-your-sheet-id-here",
    "CredentialsPath": "credentials.json"
  }
}
```

> Never commit this file. It is already blocked by `.gitignore`.
> Never put real values in `appsettings.json` — that file IS committed to GitHub.

---

### Step 6 — Set Environment in Visual Studio

This tells the app to load your `appsettings.Development.json` when you run it.

1. In Visual Studio, right-click your project in Solution Explorer → **Properties**
2. Go to **Debug** → **Open debug launch profiles UI**
3. Under **Environment variables**, click **+** and add:
   - Name: `DOTNET_ENVIRONMENT`
   - Value: `Development`
4. Save and close

---

### Step 7 — Run the App

Press **F5** in Visual Studio or click the Run button.

Expected console output:
```
📧 Templates loaded.
📋 4 recipient(s) found. Starting send...

✅ Sent [member template]  → juan@example.com (Member)
✅ Sent [other template]   → maria@example.com (President)
⏭️  Skipped                → pedro@example.com (already sent)
✅ Sent [member template]  → ana@example.com (Member)

🎉 Done. Sent: 3 | Skipped: 1 | Total: 4
```

**Console indicators:**
- `✅ Sent [member template]` — email sent using `email_template.html`
- `✅ Sent [other template]` — email sent using `email_other.html`
- `⏭️  Skipped` — Column D contains `Sent`, row was skipped
- `❌ Failed` — an error occurred for that recipient (others continue sending)


---


## Customizing the Email Templates

This project uses two templates located in the `Templates/` folder:

| File | Sent To |
|---|---|
| `email_template.html` | Recipients with position `Member` |
| `email_other.html` | Recipients with any other position |

**Available placeholders** (replaced automatically per recipient at send time):

| Placeholder | Replaced With |
|---|---|
| `{{name}}` | Recipient's name from Column B |
| `{{email}}` | Recipient's email from Column A |
| `{{position}}` | Recipient's position from Column C |

> `{{position}}` is mainly used in `email_other.html`. If it appears in `email_template.html` it will still be replaced — if it doesn't appear, it is safely ignored.

**All images must use full GitHub raw URLs** — local paths will not load in email clients.

To change the subject line, edit `Services/GmailSmtpService.cs`:
```csharp
message.Subject = "Your Subject Here";
```
---

## Security Notes

| File | Committed to GitHub | Reason |
|---|---|---|
| `appsettings.json` | Yes | Empty structure only |
| `appsettings.Development.json` | No | Your real credentials live here |
| `credentials.json` | No | Contains your Google private key |
| `.env.example` | Yes | Reference checklist — no real values |

If you accidentally commit credentials:
- **Gmail App Password**: Go to [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords) → delete it → generate a new one
- **Google credentials**: Google Cloud Console → IAM & Admin → Service Accounts → Keys → delete the key → add a new one

---

## Troubleshooting

| Error | Likely Cause | Fix |
|---|---|---|
| `❌ Missing credentials` | `appsettings.Development.json` not filled in | Fill in all values; confirm `DOTNET_ENVIRONMENT=Development` is set |
| `❌ email_template.html not found` | File not copied to output | Right-click file in VS → Properties → Copy if Newer |
| `Authentication failed` | Wrong App Password | Re-generate at myaccount.google.com/apppasswords |
| `⚠️ No recipients found` | Sheet not shared or wrong Sheet ID | Share sheet with service account email; check Sheet ID |
| `403 Forbidden` (Sheets) | API not enabled or wrong project | Enable Sheets API in Google Cloud Console |
| Images broken in email | Using local paths | Replace all image `src` with GitHub raw URLs |
| Gmail throttling | Sending too fast | Increase `Task.Delay` in `Program.cs` to 3000ms |

---

## Daily Send Limits

| Account Type | Daily Limit |
|---|---|
| Free Gmail (`@gmail.com`) | 500 emails/day (hard cap) |
| Recommended safe range | Under 100 emails/day |

The app adds a 1.5-second delay between each email by default.
