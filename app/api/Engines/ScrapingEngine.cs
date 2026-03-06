using System.Text.RegularExpressions;

using Api.DomainModels;

namespace Api.Engines;

public sealed class ScrapingEngine(IHttpClientFactory httpClientFactory)
{
    private const string ListUrl = "https://petbridge.org/animals/animals-all-responsive.php?ClientID=2&Species=Dog";
    private const string DetailUrlTemplate = "https://petbridge.org/animals/animals-detail.php?ID={0}&ClientID=2&Species=Dog";
    private const string ProfileUrlTemplate = "https://kshumane.org/adoption/pet-details/?aid={0}&cid=2&tid=Dog";

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

    private static readonly Regex DaysOldRegex = new(
        @"days_old_(\d+)", RegexOptions.Compiled);

    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PetBridge");
        var html = await client.GetStringAsync(ListUrl, ct);
        var cards = CardRegex.Matches(html);
        var dogs = new List<Dog>();

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
            var profileUrl = String.Format(ProfileUrlTemplate, aid);
            var daysOldMatch = DaysOldRegex.Match(cardHtml);
            int? daysAtShelter = daysOldMatch.Success ? Int32.Parse(daysOldMatch.Groups[1].Value) : null;

            dogs.Add(new Dog(aid, name, age, gender, photoUrl, null, profileUrl, default, daysAtShelter));
        }

        return dogs;
    }

    public async Task<string?> GetDogBreedAsync(string aid, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PetBridge");
        var url = String.Format(DetailUrlTemplate, aid);

        try
        {
            var html = await client.GetStringAsync(url, ct);
            var match = BreedRegex.Match(html);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
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
