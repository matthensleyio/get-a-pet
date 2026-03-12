using System.Text.RegularExpressions;

using Api.DomainModels;

namespace Api.Engines;

public sealed class ScrapingEngine(IHttpClientFactory httpClientFactory, IReadOnlyList<ShelterConfig> shelters)
{
    private const string ListUrlTemplate = "https://petbridge.org/animals/animals-all-responsive.php?ClientID={0}&Species=Dog";
    private const string DetailUrlTemplate = "https://petbridge.org/animals/animals-detail.php?ID={0}&ClientID={1}&Species=Dog";

    private static readonly Regex CardRegex = new(
        @"(?s)<div class=""animal_list_box[^""]*""[^>]*>.*?</div>\s*</div>\s*<!-- animal_list_box -->",
        RegexOptions.Compiled);

    private static readonly Regex AidRegex = new(
        @"aid=(\d+)", RegexOptions.Compiled);

    private static readonly Regex NameRegex = new(
        @"class=""results_animal_link"">([^<]+)", RegexOptions.Compiled);

    private static readonly Regex AgeRegex = new(
        @"results_animal_detail_data_Age"">([^<]+)<", RegexOptions.Compiled);

    private static readonly Regex GenderRegex = new(
        @"results_animal_detail_data_Sex"">([^<]+)<", RegexOptions.Compiled);

    private static readonly Regex PhotoRegex1 = new(
        @"class=""results_animal_image""[^>]*src=""([^""]+)""", RegexOptions.Compiled);

    private static readonly Regex PhotoRegex2 = new(
        @"src=""([^""]+)""[^>]*class=""results_animal_image""", RegexOptions.Compiled);

    private static readonly Regex PhotoRegex3 = new(
        @"<img[^>]+src=""(https://g\.petango\.com[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex BreedRegex = new(
        @"Breed:</span>\s*([^<]+)", RegexOptions.Compiled);

    private static readonly Regex ColorRegex = new(
        @"Color:</span>\s*([^<]+)", RegexOptions.Compiled);

    private static readonly Regex SizeRegex = new(
        @"Size:</span>\s*([^<]+)", RegexOptions.Compiled);

    private static readonly Regex WeightRegex = new(
        @"Weight:</span>\s*([^<]+)", RegexOptions.Compiled);

    private static readonly Regex AdoptionFeeRegex = new(
        @"Adoption Fee:</strong>\s*([^<]+)", RegexOptions.Compiled);

    private static readonly Regex LocationRegex = new(
        @"Location:</span>\s*([^<]+)", RegexOptions.Compiled);

    private static readonly Regex DaysOldRegex = new(
        @"days_old_(\d+)", RegexOptions.Compiled);

    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        var tasks = shelters
            .Select(shelter => GetDogsForShelterAsync(shelter, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(d => d).ToList();
    }

    public async Task<bool> IsStillAvailableAsync(string aid, string shelterId, CancellationToken ct)
    {
        var detail = await GetDogDetailAsync(aid, shelterId, ct);
        return detail is { } d &&
               (d.Breed is not null || d.Color is not null || d.Size is not null ||
                d.Weight is not null || d.AdoptionFee is not null || d.CurrentLocation is not null);
    }

    public async Task<DogDetail?> GetDogDetailAsync(string aid, string shelterId, CancellationToken ct)
    {
        var shelter = shelters.First(s => s.ShelterId == shelterId);
        var client = httpClientFactory.CreateClient("PetBridge");
        var url = String.Format(DetailUrlTemplate, aid, shelter.PetBridgeClientId);
        var intakeDatePattern = new Regex(
            Regex.Escape(shelter.IntakeDateLabel) + @"</span>\s*([A-Za-z]+ \d+, \d+)",
            RegexOptions.Compiled);

        try
        {
            var html = await client.GetStringAsync(url, ct);

            var intakeDateStr = ExtractGroup(intakeDatePattern, html)?.Trim();
            DateTimeOffset? intakeDate = intakeDateStr is not null
                && DateTimeOffset.TryParse(intakeDateStr, out var parsed)
                ? parsed
                : null;

            return new DogDetail(
                ExtractGroup(BreedRegex, html)?.Trim(),
                ExtractGroup(ColorRegex, html)?.Trim(),
                ExtractGroup(SizeRegex, html)?.Trim(),
                ExtractGroup(WeightRegex, html)?.Trim(),
                ExtractGroup(AdoptionFeeRegex, html)?.Trim(),
                ExtractGroup(LocationRegex, html)?.Trim(),
                intakeDate);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<Dog>> GetDogsForShelterAsync(ShelterConfig shelter, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PetBridge");
        var url = String.Format(ListUrlTemplate, shelter.PetBridgeClientId);
        var html = await client.GetStringAsync(url, ct);
        var cards = CardRegex.Matches(html);
        var dogs = new List<Dog>();
        var today = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);

        foreach (Match card in cards)
        {
            var cardHtml = card.Value;
            var aidMatch = AidRegex.Match(cardHtml);

            if (!aidMatch.Success)
            {
                continue;
            }

            var aid = aidMatch.Groups[1].Value;
            var name = ExtractGroup(NameRegex, cardHtml);
            var age = ExtractGroup(AgeRegex, cardHtml);
            var gender = ExtractGroup(GenderRegex, cardHtml);
            var photoUrl = ExtractPhotoUrl(cardHtml);
            var profileUrl = String.Format(shelter.ProfileUrlTemplate, aid);
            var daysOldMatch = DaysOldRegex.Match(cardHtml);
            DateTimeOffset? listingDate = daysOldMatch.Success
                ? today.AddDays(-int.Parse(daysOldMatch.Groups[1].Value))
                : null;

            dogs.Add(new Dog(aid, shelter.ShelterId, name, age, gender, photoUrl, null, null, null, null, null, null, profileUrl, default, null, listingDate));
        }

        return dogs;
    }

    private static string? ExtractGroup(Regex regex, string input)
    {
        var match = regex.Match(input);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractPhotoUrl(string cardHtml)
    {
        var match = PhotoRegex1.Match(cardHtml);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = PhotoRegex2.Match(cardHtml);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = PhotoRegex3.Match(cardHtml);
        return match.Success ? match.Groups[1].Value : null;
    }
}
