# Microsoft Foundry (Azure AI) Verification Report — June 2026

This document verifies the claims made in `.omo/handoff.md` and `AGENTS.md` regarding tool names, pricing, and deprecation schedules against live data as of June 2026.

## 1. Platform Rebranding
- **Claim:** "Azure AI Studio" rebranded to "Microsoft Foundry", and "Azure AI Services" rebranded to "Foundry Tools".
- **Verification:** **CONFIRMED**. Microsoft rebranded its enterprise AI platform to Microsoft Foundry in late 2025.

## 2. Service Verifications

### Azure Translator (Foundry Tools — Translator)
- **Claim:** v3.0 API retiring Q3 2026 (requires migrating to `2026-06-06` GA).
- **Verification:** **DISCREPANCY DETECTED**. While a new `2026-06-06` unified text translation API was indeed released to add LLM-based capabilities, there is **no official announcement** forcing the retirement of the v3.0 GA endpoint in Q3 2026. Existing customers can continue using v3.0.
- **Pricing Claim:** 2M chars/mo free; $10/1M chars.
- **Verification:** **CONFIRMED**.

### Azure Document Intelligence (Foundry Tools — Document Intelligence)
- **Claim:** v4.0 is active; v2.0/v2.1/v3.0 are retiring.
- **Verification:** **CONFIRMED**. Older versions are scheduled for retirement between 2026 and 2029. v4.0 is the current supported version.
- **Pricing Claim:** 500 pages/mo free; $1.50/1k pages (Read), $10/1k pages (Layout/Prebuilt).
- **Verification:** **CONFIRMED**.

### Azure Vision Image Analysis (Foundry Tools — Image Analysis)
- **Claim:** v4.0 API retires September 25, 2028.
- **Verification:** **CONFIRMED**. Microsoft has announced the retirement of the Image Analysis REST API for this date.
- **Pricing Claim:** 5,000 transactions/mo free; $1.00/1k (Group 1), $1.50/1k (Group 2).
- **Verification:** **CONFIRMED**.

### Azure OpenAI (Foundry Models)
- **Claim:** `gpt-4o-mini` is to be replaced by `gpt-4.1-mini` by October 2026.
- **Verification:** **CONFIRMED**. `gpt-4.1-mini` is now generally available (GA) on Azure AI Foundry and is the recommended forward path, featuring an expanded 1 million token context window.
- **Pricing Claim:** `gpt-4o-mini` is $0.15/1M input, $0.60/1M output.
- **Verification:** **CONFIRMED**. 

## 3. Free Tier Duration & Billing Mechanics (Month 1-12 vs. Month 13+)

It is crucial to differentiate between Azure's "12-Months Free" services (like basic VMs and storage) and "Always Free" services (like the AI F0 tiers):

| Phase | "12-Months Free" Services (e.g., VMs, SQL) | "Always Free" Services (Translator, Vision, Doc Intel F0) |
| :--- | :--- | :--- |
| **Months 1–12** | Free up to specific monthly limits. | Free up to the F0 monthly limit (e.g., 2M chars/mo). |
| **Month 13+** | **EXPIRES.** You are automatically billed at Pay-As-You-Go rates for ongoing usage. | **REMAINS FREE.** The F0 tier never expires. You continue getting the free monthly quota. |
| **Overage Behavior** | If you exceed limits, you are billed for the overage. | If you exceed the F0 limit, the API simply **blocks requests** (Out of Quota) until the next month. You are **never billed** on an F0 resource. To process more, you must manually upgrade to the S1 tier. |

**The AI Services Verified:**
- **Azure Translator:** The F0 tier is **Permanent ("Always Free")**. 2M characters/month indefinitely.
- **Azure Document Intelligence:** The F0 tier is **Permanent ("Always Free")**. *(Note: F0 limits extraction to the first 2 pages of a document).*
- **Azure Vision Image Analysis:** The F0 tier is **Permanent ("Always Free")**, granting 5,000 transactions/month indefinitely.

*(Note: While the AI tiers remain free forever, Microsoft requires an active subscription—such as a Pay-As-You-Go account—to keep the account open after the initial trial ends.)*

## Summary
The documentation in `AGENTS.md` and `.omo/handoff.md` is extremely accurate for the June 2026 state, with one notable exception regarding the Azure Translator API. The forced deprecation of v3.0 is a false alarm, although upgrading to the `2026-06-06` API is still beneficial for LLM access.

**Action Taken:** 
- The `.omo/PLAN.md` file has been stripped of all reference documentation, pricing charts, and completed/deferred noise. It now strictly contains pending implementation checkboxes.
