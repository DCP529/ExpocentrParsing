using AngleSharp;
using AngleSharp.Html.Parser;

class Program
{
    static async Task Main()
    {
        try
        {
            var baseUrl = "https://www.expocentr.ru/ru/events/?PAGEN_1=";
            var pageNumber = 1;

            while (pageNumber <= 13)
            {
                var url = baseUrl + pageNumber;
                var html = await DownloadPageAsync(url);
                var events = ParseEvents(html);

                if (events.Count == 0)
                {
                    break;
                }

                foreach (var ev in events)
                {
                    Console.WriteLine($"Дата: {ev.Date}");
                    Console.WriteLine($"Название: {ev.Title}");
                    Console.WriteLine($"Тип: {ev.Type}");
                    Console.WriteLine($"Организатор: {ev.Organizer}");
                    Console.WriteLine($"Описание: {ev.Description}");

                    if (!string.IsNullOrWhiteSpace(ev.Link))
                    {
                        var link = url.Replace($"/ru/events/?PAGEN_1=" + pageNumber, ev.Link);
                        var eventDetails = await GetFullEventInfoAsync(link);
                        if (eventDetails != null)
                        {
                            if (eventDetails != null)
                            {
                                Console.WriteLine("Даты проведения:");
                                foreach (var date in eventDetails.Dates)
                                {
                                    Console.WriteLine($"- {date}");
                                }

                                Console.WriteLine($"Монтаж: {eventDetails.Montage}");

                                Console.WriteLine($"Демонтаж: {eventDetails.Demontage}");

                                Console.WriteLine($"Место проведения: {eventDetails.Place}");
                                Console.WriteLine($"{ev.Photos[0]}") ;
                            }
                        }
                    }

                    Console.WriteLine("=====================================");
                }

                pageNumber++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Данные выгружены успешно");
            Console.ReadLine();
        }
    }

    static async Task<string> DownloadPageAsync(string url)
    {
        using (var httpClient = new HttpClient())
        {
            return await httpClient.GetStringAsync(url);
        }
    }

    static List<Event> ParseEvents(string html)
    {
        var events = new List<Event>();
        var context = BrowsingContext.New(Configuration.Default);
        var parser = context.GetService<IHtmlParser>();
        var document = parser.ParseDocument(html);

        var eventNodes = document.QuerySelectorAll(".event-card-full");
        foreach (var eventNode in eventNodes)
        {
            var eventNew = new Event();
            var dateNode = eventNode.QuerySelector(".event-card-full__date");
            var titleNode = eventNode.QuerySelector(".event-card-full__title");
            var typeNode = eventNode.QuerySelector(".event-card-full__type");
            var organizerNode = eventNode.QuerySelector(".event-card-full__organizer-name");
            var descriptionNode = eventNode.QuerySelector(".event-card-full__descr");
            var linkNode = eventNode.QuerySelector("a.event-card-full__content");
            var photoNodes = document.QuerySelectorAll(".event-card-full__logo img");

            eventNew = new Event
            {
                Date = dateNode?.TextContent.Trim(),
                Title = titleNode?.TextContent.Trim(),
                Type = typeNode?.TextContent.Trim(),
                Organizer = organizerNode?.TextContent.Trim(),
                Description = descriptionNode?.TextContent.Trim(),
                Link = linkNode?.GetAttribute("href")
            };

            if (photoNodes != null && photoNodes.Length > 0)
            {
                eventNew.Photos = photoNodes.Select(img => img.GetAttribute("src")).ToList();
            }

            events.Add(eventNew);
        }

        return events;
    }

    static async Task<EventDetails> GetFullEventInfoAsync(string url)
    {
        try
        {
            var html = await DownloadPageAsync(url);
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();
            var document = parser.ParseDocument(html);

            var eventDetails = new EventDetails();

            var dateNodes = document.QuerySelectorAll(".item-dates__info");
            var timeNodes = document.QuerySelectorAll(".item-dates__time");

            // Извлечь первые две даты монтажа и демонтажа
            if (dateNodes.Length >= 2 && timeNodes.Length >= 2)
            {
                eventDetails.Dates.Add($"{dateNodes[0].TextContent.Trim()} {timeNodes[0].TextContent.Trim()}");
                eventDetails.Dates.Add($"{dateNodes[1].TextContent.Trim()} {timeNodes[1].TextContent.Trim()}");
            }

            // Извлечь третью и четвёртую даты монтажа и демонтажа
            if (dateNodes.Length >= 4 )
            {
                eventDetails.Montage = $"{dateNodes[2].TextContent.Trim()}";
                eventDetails.Demontage = $"{dateNodes[3].TextContent.Trim()}";
            }

            var placeNode = document.QuerySelector(".item-place__pav");
            if (placeNode != null)
            {
                eventDetails.Place = placeNode.TextContent.Trim();
            }

            


            return eventDetails;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении информации с {url}: {ex.Message}");
            return null;
        }
    }

}


class EventDetails
{
    public List<string> Dates { get; } = new List<string>();
    public string Montage { get; set; }
    public string Demontage { get; set; }
    public string Place { get; set; }
}




class Event
{
    public string Date { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string Organizer { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }


    public List<string> Photos { get; set; } = new List<string>();
}
