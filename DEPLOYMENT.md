# Deployment Guide (Free Tier)

This guide helps you deploy **ExtraTime** to Azure (Backend) and Supabase (Database) for free (or near-free), minimizing usage to avoid costs.

## 1. Database (Supabase)
Supabase provides a generous free tier for PostgreSQL.

1.  **Create Account:** Go to [supabase.com](https://supabase.com/) and sign up.
2.  **New Project:** Create a new project. Give it a name and secure password.
3.  **Get Connection String:**
    *   Go to **Project Settings** -> **Database**.
    *   Under **Connection String** -> **URI**, copy the value.
    *   *Important:* Replace `[YOUR-PASSWORD]` with the password you set in step 2.
    *   Save this string; you will need it for GitHub Secrets.

## 2. Backend (Azure App Service)
We will use the **F1 Free Tier** which gives 60 minutes of CPU time per day.

1.  **Create Azure Account:** Go to [portal.azure.com](https://portal.azure.com).
2.  **Create Web App:**
    *   Search for "App Services" -> "Create" -> "Web App".
    *   **Resource Group:** Create new (e.g., `rg-extratime`).
    *   **Name:** Unique name (e.g., `extratime-app`).
    *   **Publish:** Code.
    *   **Runtime stack:** .NET 9 (or .NET 10 if available/preview).
    *   **Operating System:** Windows (F1 is most reliable on Windows).
    *   **Region:** Choose close to you.
    *   **Pricing Plan:** Click "Explore pricing plans" or "Change size". Select **Free F1**.
3.  **Review & Create.**
4.  **Configuration (Env Vars):**
    *   Go to your new Web App -> **Settings** -> **Environment variables**.
    *   Add `ConnectionStrings__DefaultConnection` and set the value to your Supabase connection string.
    *   Add `Jwt__Secret` and set a long, random secret key.
    *   Add `FootballData__ApiKey` if you have one.
    *   Click **Apply**.

## 3. GitHub Actions Setup
Connect your code to the cloud.

1.  **Get Publish Profile:**
    *   In Azure Portal (Web App), click **"Get publish profile"** (top toolbar) or "Download publish profile". This downloads a file.
    *   Open the file with a text editor (Notepad/VS Code) and copy the XML content.
2.  **Set Secrets in GitHub:**
    *   Go to your GitHub Repository -> **Settings** -> **Secrets and variables** -> **Actions**.
    *   **New Repository Secret**:
        *   Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
        *   Value: (Paste the XML content).
    *   **New Repository Secret**:
        *   Name: `DB_CONNECTION_STRING`
        *   Value: (Paste your Supabase connection string).

## 4. Cost "Locks" & Monitoring
To ensure you don't pay accidentally:

1.  **Azure Spending Limit:** If you are on a "Pay-as-you-go" subscription (not a student/starter credit trial), search for "Cost Management + Billing". You can set budgets, but "hard stops" are tricky. However, the **F1 plan** simply stops the app if you exceed the daily 60 CPU minutes. It does not auto-scale to paid tiers.
2.  **Supabase Spend Cap:** Ensure the "Spend Cap" is enabled (it is by default on the Free Plan). This pauses the project if limits are exceeded.

## 5. Offline Behavior
*   **Azure:** If your app receives too much traffic and hits the 60-minute CPU quota, Azure will stop the site until the next day. It returns a 403 or Service Unavailable. This meets your "offline rather than pay" requirement.
*   **Supabase:** Use of the database pauses after 1 week of inactivity. You just need to log in to the dashboard to wake it up, or the first request might fail/timeout while waking up.

## 6. Optimization for Free Tier
Because you have a `FootballSyncHostedService` running every 5 minutes during match hours, you need to be careful with CPU usage.

*   **Disable "Always On":** This is not available in Free tier anyway, but ensure your app can sleep.
*   **Reduce Sync Frequency:** If you hit limits, consider changing `LiveSyncInterval` in `FootballSyncHostedService.cs` from 5 minutes to 15 or 30 minutes.
