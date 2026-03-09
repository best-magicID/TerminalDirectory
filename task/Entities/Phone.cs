namespace task.Entities
{
    public class Phone
    {
        /// <summary>
        /// ID (первичный ключ)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Номер телефона
        /// </summary>
        public string Number { get; set; } = "";
    }
}
