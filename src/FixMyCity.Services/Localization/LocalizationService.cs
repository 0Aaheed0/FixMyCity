using System;
using System.Collections.Generic;
using System.Globalization;

namespace FixMyCity.Services.Localization
{
    public interface ILocalizationService
    {
        bool IsBangla(string? cultureCode = null);
        string T(string key, bool? forceBangla = null);
        string ToBanglaDigits(string? input);
        string FormatCurrency(decimal amount, bool? forceBangla = null);
        string FormatDate(DateTime date, bool? forceBangla = null);
    }

    public class LocalizationService : ILocalizationService
    {
        private static readonly Dictionary<string, string> Translations = new(StringComparer.OrdinalIgnoreCase)
        {
            // Navigation
            ["Nav_Dashboard"] = "Dashboard",
            ["Nav_ReportIssue"] = "Report Issue",
            ["Nav_MyReports"] = "My Reports",
            ["Nav_Map"] = "Problem Map",
            ["Nav_MunicipalGateway"] = "Municipal Gateway",
            ["Nav_Announcements"] = "Announcements",
            ["Nav_EmergencyAlerts"] = "Emergency Alerts",
            ["Nav_LostFound"] = "Lost & Found",
            ["Nav_Notifications"] = "Notifications",
            ["Nav_Profile"] = "Profile",
            ["Nav_Logout"] = "Logout",
            ["Nav_Login"] = "Log In",
            ["Nav_Register"] = "Sign Up",
            ["Nav_About"] = "About Us",
            ["Nav_Privacy"] = "Privacy Policy",
            ["Nav_Admin"] = "Admin Command",
            ["Nav_Staff"] = "Staff Console",
            ["Nav_Manager"] = "Manager Console",

            // Quick Actions / Buttons
            ["Btn_Submit"] = "Submit",
            ["Btn_Cancel"] = "Cancel",
            ["Btn_PayNow"] = "Pay Now",
            ["Btn_Search"] = "Search",
            ["Btn_Filter"] = "Filter",
            ["Btn_Details"] = "Details",
            ["Btn_ViewMap"] = "View on Map",
            ["Btn_MarkRead"] = "Mark as Read",
            ["Btn_MarkAllRead"] = "Mark All as Read",
            ["Btn_DownloadReceipt"] = "Download Receipt",
            ["Btn_DetectLocation"] = "Detect My Location",
            ["Btn_PickOnMap"] = "Pick Location on Map",
            ["Btn_CreatePost"] = "Create Post",
            ["Btn_Respond"] = "Send Response",

            // Statuses
            ["Status_Reported"] = "Reported",
            ["Status_InProgress"] = "In Progress",
            ["Status_Resolved"] = "Resolved",
            ["Status_Verified"] = "Verified",
            ["Status_Rejected"] = "Rejected",

            // Categories
            ["Cat_Roads"] = "Roads & Pavements",
            ["Cat_Waste"] = "Garbage & Sanitation",
            ["Cat_Water"] = "Water & Drainage",
            ["Cat_Electricity"] = "Street Lighting & Power",
            ["Cat_Parks"] = "Parks & Environment",

            // Municipal Gateway
            ["Gateway_Title"] = "Digital Municipal Services & Utility Gateway",
            ["Gateway_Subtext"] = "Official digital bill payment portal for utilities, city corporation holding tax, and municipal fees across Dhaka.",
            ["Gateway_BillsDue"] = "Pending Bills",
            ["Gateway_BillsPaid"] = "Paid Bills",
            ["Gateway_AmountPaid"] = "Total Paid",
            ["Gateway_SearchBill"] = "Search Bill by Consumer Number",
            ["Gateway_SelectProvider"] = "Select Service Provider",
            ["Gateway_PayBill"] = "Pay Municipal Bill",
            ["Gateway_Receipt"] = "Official Municipal Payment Receipt",

            // Community & Announcements
            ["Announce_Title"] = "Public Announcements",
            ["Announce_Subtext"] = "Official municipal updates, road closures, and utility maintenance alerts.",
            ["Alert_Emergency"] = "EMERGENCY ALERT",
            ["Alert_AllCitizens"] = "Broadcasted to all citizens across Dhaka",

            // Lost and Found
            ["LF_Title"] = "Lost & Found Community",
            ["LF_Subtext"] = "Help your neighbors recover lost valuables or return found items safely."
        };

        public bool IsBangla(string? cultureCode = null) => false;

        public string T(string key, bool? forceBangla = null)
        {
            if (Translations.TryGetValue(key, out var val))
            {
                return val;
            }
            return key;
        }

        public string ToBanglaDigits(string? input) => input ?? string.Empty;

        public string FormatCurrency(decimal amount, bool? forceBangla = null)
        {
            return $"BDT {amount.ToString("N2", CultureInfo.InvariantCulture)}";
        }

        public string FormatDate(DateTime date, bool? forceBangla = null)
        {
            return date.ToLocalTime().ToString("MMM dd, yyyy HH:mm");
        }
    }
}
