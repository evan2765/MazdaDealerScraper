using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Dealer
{
    public string Name { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string AddressLine3 { get; set; } = "";
    public string CityTown { get; set; }
    public string County { get; set; } = "";
    public string Postcode { get; set; }
    public string PhoneNumber { get; set; }
    public string ExternalReferences { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public string WebsiteUrl { get; set; }
    public string IsActive { get; set; }
}

class Program
{
    static async Task Main()
    {
        var httpClient = new HttpClient();
        string url = "https://www.mazda.co.uk/api/dealers";
        var json = await httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var dealers = doc.RootElement.GetProperty("data").GetProperty("dealers");

        List<Dealer> dealerList = new();

        foreach (var d in dealers.EnumerateArray())
        {
            try
            {
                var address = d.GetProperty("address");
                var contact = d.GetProperty("contact");

                // Latitude/Longitude default
                double latitude = 0, longitude = 0;
                if (address.TryGetProperty("coordinates", out var coords))
                {
                    if (coords.TryGetProperty("latitude", out var latElem))
                        latitude = latElem.GetDouble();
                    if (coords.TryGetProperty("longitude", out var lonElem))
                        longitude = lonElem.GetDouble();
                }

                string phone = "";
                if (contact.TryGetProperty("phoneNumber", out var phoneObj) &&
                    phoneObj.TryGetProperty("default", out var phoneVal))
                    phone = phoneVal.GetString() ?? "";

                string website = contact.TryGetProperty("website", out var w) ? w.GetString() ?? "" : "";

                dealerList.Add(new Dealer
                {
                    Name = d.GetProperty("name").GetString() ?? "",
                    AddressLine1 = address.TryGetProperty("address1", out var a1) ? a1.GetString() ?? "" : "",
                    AddressLine2 = address.TryGetProperty("address2", out var a2)
                        ? a2.GetString() ?? ""
                        : (address.TryGetProperty("street2", out var s2) ? s2.GetString() ?? "" : ""),
                    CityTown = address.TryGetProperty("city", out var city) ? city.GetString() ?? "" : "",
                    Postcode = address.TryGetProperty("postcode", out var pc) ? pc.GetString() ?? "" : "",
                    PhoneNumber = phone,
                    ExternalReferences = d.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    Latitude = latitude,
                    Longitude = longitude,
                    WebsiteUrl = website,
                    IsActive = d.TryGetProperty("active", out var active) && active.GetBoolean() ? "TRUE" : "FALSE"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Skipped a dealer due to error: {ex.Message}");
            }
        }

        // Write to CSV
        string filePath = "mazda_dealers.csv";
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("WorkspaceOrganisationReference;Name;AddressLine1;AddressLine2;AddressLine3;CityTown;County;Postcode;Type;Franchise;IsActive;PhoneNumber;ExternalReferences;Longitude;Latitude;WebsiteUrl");

        foreach (var dealer in dealerList)
        {
            writer.WriteLine($";" + // WorkspaceOrganisationReference = blank
                             $"{dealer.Name};" +
                             $"{dealer.AddressLine1};" +
                             $"{dealer.AddressLine2};" +
                             $"{dealer.AddressLine3};" +
                             $"{dealer.CityTown};" +
                             $"{dealer.County};" +
                             $"{dealer.Postcode};" +
                             $"Dealer;" + // Type
                             $"Mazda;" + // Franchise
                             $"{dealer.IsActive};" +
                             $"{dealer.PhoneNumber};" +
                             $"{dealer.ExternalReferences};" +
                             $"{dealer.Longitude};" +
                             $"{dealer.Latitude};" +
                             $"{dealer.WebsiteUrl}");
        }

        Console.WriteLine("✅ Mazda dealer data written to mazda_dealers.csv");
    }
}