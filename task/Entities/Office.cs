using System.Text.Json.Serialization;

namespace TerminalDirectory.Entities;

public class Office
{
    [JsonIgnore]
    public int Id { get; set; }

    public string? Code { get; set; }

    public int CityCode { get; set; }

    public string? Uuid { get; set; }

    public OfficeType? Type { get; set; }

    public string CountryCode { get; set; }

    public Coordinates Coordinates { get; set; }

    public string? AddressRegion { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressHouseNumber { get; set; }
    public int? AddressApartment { get; set; }

    public string WorkTime { get; set; }

    public List<Phone> Phones { get; set; } = new();
}
//public class Office
//{
//    /// <summary>
//    /// ID (первичный ключ)
//    /// </summary>
//    public int Id { get; set; }

//    /// <summary>
//    /// Код терминала (уникальный идентификатор терминала в рамках города)
//    /// </summary>
//    public string? Code { get; set; }

//    /// <summary>
//    /// Код города (идентификатор города, к которому относится терминал)
//    /// </summary>
//    public int CityCode { get; set; }

//    /// <summary>
//    /// 
//    /// </summary>
//    public string? Uuid { get; set; }

//    /// <summary>
//    /// Тип офиса 
//    /// </summary>
//    public OfficeType? Type { get; set; }

//    /// <summary>
//    /// Код страны
//    /// </summary>
//    public string CountryCode { get; set; }

//    /// <summary>
//    /// Координаты
//    /// </summary>
//    public Coordinates Coordinates { get; set; } = new Coordinates();

//    /// <summary>
//    /// Адрес
//    /// </summary>
//    public Address Address { get; set; } = new Address();

//    /// <summary>
//    /// Время работы
//    /// </summary>
//    public string WorkTime { get; set; } = "";

//    /// <summary>
//    /// Телефоны
//    /// </summary>
//    public List<Phone> Phones { get; set; } = new List<Phone>();
//}
