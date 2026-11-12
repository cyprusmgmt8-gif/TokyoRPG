/// <summary>
/// The places on the city map. Add a new entry here when a room is planned.
/// Only a place with a scene name can be entered. includedInFreeVersion stays open without premium.
/// </summary>
public class CityLocation
{
    public string id;
    public string englishName;
    public string japaneseName;
    public string reading;
    public string summary;
    public bool includedInFreeVersion;
    public string sceneName;

    public CityLocation(
        string id,
        string englishName,
        string japaneseName,
        string reading,
        string summary,
        bool includedInFreeVersion,
        string sceneName)
    {
        this.id = id;
        this.englishName = englishName;
        this.japaneseName = japaneseName;
        this.reading = reading;
        this.summary = summary;
        this.includedInFreeVersion = includedInFreeVersion;
        this.sceneName = sceneName;
    }
}

public static class CityDirectory
{
    public static readonly CityLocation[] All =
    {
        new CityLocation("bedroom", "Bedroom", "寝室", "しんしつ", "Everyday words in your room.", true, GameScenes.Bedroom),
        new CityLocation("convenience-store", "Convenience Store", "コンビニ", "コンビニ", "Shopping and short polite phrases.", false, GameScenes.Konbini),
        new CityLocation("station", "Train Station", "駅", "えき", "Tickets, platforms and asking the way.", false, ""),
        new CityLocation("cafe", "Café", "カフェ", "カフェ", "Ordering a drink and a snack.", false, ""),
        new CityLocation("restaurant", "Restaurant", "レストラン", "レストラン", "Reading a menu and ordering food.", false, ""),
        new CityLocation("supermarket", "Supermarket", "スーパー", "スーパー", "Food names and prices.", false, ""),
        new CityLocation("school", "School", "学校", "がっこう", "Classroom words and simple questions.", false, ""),
        new CityLocation("park", "Park", "公園", "こうえん", "Weather, hobbies and meeting people.", false, ""),
        new CityLocation("shrine", "Shrine", "神社", "じんじゃ", "Visits, manners and festival words.", false, ""),
        new CityLocation("bookstore", "Bookstore", "本屋", "ほんや", "Books, magazines and asking for a title.", false, ""),
        new CityLocation("clinic", "Clinic", "病院", "びょういん", "Symptoms and making an appointment.", false, ""),
        new CityLocation("post-office", "Post Office", "郵便局", "ゆうびんきょく", "Letters, parcels and forms.", false, ""),
        new CityLocation("office", "Office", "会社", "かいしゃ", "Workdays, meetings and phone calls.", false, ""),
        new CityLocation("shopping-street", "Shopping Street", "商店街", "しょうてんがい", "Shops, directions and prices.", false, ""),
        new CityLocation("city-hall", "City Hall", "市役所", "しやくしょ", "Addresses, forms and public services.", false, "")
    };
}
