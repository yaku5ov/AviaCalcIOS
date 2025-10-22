using Microsoft.Maui;

namespace AviaCalc;

public partial class MainPage : ContentPage
{
    // Константы (в кг)
    private const int INITIAL_FUEL = 1000;
    private const int DRAIN_WITH_AUX = 8;
    private const int DRAIN_WITHOUT_AUX = 4;
    private const int FINAL_FUEL = 1000;
    private const int GROUND_CONSUMPTION = 185;
    private const int AIR_CONSUMPTION = 325;

    public MainPage()
    {
        InitializeComponent();

        // Подписываемся на события
        AuxYesRadio.CheckedChanged += OnAuxRadioChanged;
        AuxNoRadio.CheckedChanged += OnAuxRadioChanged;
        CalculateBtn.Clicked += OnCalculateClicked;
        ClearBtn.Clicked += OnClearClicked;

        // Инициализируем таблицу начальными значениями
        InitializeTable();
    }

    private void InitializeTable()
    {
        DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy");
        // Устанавливаем начальные значения
        BeforeFlightLabel.Text = INITIAL_FUEL.ToString();
        DrainedDocLabel.Text = "-";
        AfterFlightLabel.Text = FINAL_FUEL.ToString();
    }

    private void OnAuxRadioChanged(object sender, CheckedChangedEventArgs e)
    {
        AuxInputLayout.IsVisible = AuxYesRadio.IsChecked;
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        try
        {
            // Получаем и конвертируем время
            double groundMinutes = GetTimeInMinutes(GroundTimeEntry.Text);
            double airMinutes = GetTimeInMinutes(AirTimeEntry.Text);

            if (groundMinutes < 0 || airMinutes < 0)
            {
                await DisplayAlert("Ошибка", "Неправильный формат времени", "OK");
                return;
            }

            // Проверяем основные баки
            if (string.IsNullOrEmpty(MainQtyEntry.Text) || string.IsNullOrEmpty(MainDensityEntry.Text) || string.IsNullOrEmpty(MainDocEntry.Text))
            {
                await DisplayAlert("Ошибка", "Заполните все поля для основных баков", "OK");
                return;
            }

            double mainQtyL = double.Parse(MainQtyEntry.Text);
            double mainDensity = double.Parse(MainDensityEntry.Text);
            string mainDoc = MainDocEntry.Text;

            // Пересчет в кг
            double mainQtyKg = mainQtyL * mainDensity;

            // Обрабатываем концевые баки
            bool auxFueled = AuxYesRadio.IsChecked;
            double auxQtyKg = 0;
            string auxDoc = "-";
            int drainBeforeFlight = auxFueled ? DRAIN_WITH_AUX : DRAIN_WITHOUT_AUX;

            if (auxFueled)
            {
                if (string.IsNullOrEmpty(AuxQtyEntry.Text) || string.IsNullOrEmpty(AuxDensityEntry.Text) || string.IsNullOrEmpty(AuxDocEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Заполните все поля для концевых баков", "OK");
                    return;
                }

                double auxQtyL = double.Parse(AuxQtyEntry.Text);
                double auxDensity = double.Parse(AuxDensityEntry.Text);
                auxDoc = AuxDocEntry.Text;
                auxQtyKg = auxQtyL * auxDensity;
            }

            // РАСЧЕТЫ
            double fuelBeforeStart = INITIAL_FUEL + auxQtyKg - drainBeforeFlight;
            double consumedInFlight = auxFueled ? mainQtyKg + auxQtyKg - DRAIN_WITH_AUX : mainQtyKg - DRAIN_WITHOUT_AUX;
            double groundConsumption = (GROUND_CONSUMPTION / 60.0) * groundMinutes;
            double airConsumption = (AIR_CONSUMPTION / 60.0) * airMinutes;
            double prescribedConsumption = groundConsumption + airConsumption;
            double economy = prescribedConsumption - consumedInFlight;

            // ОБНОВЛЯЕМ ТАБЛИЦУ
            UpdateResultTable(
                groundMinutes, airMinutes, mainDensity,
                auxQtyKg, auxDoc, drainBeforeFlight,
                fuelBeforeStart, consumedInFlight,
                mainQtyKg, mainDoc, prescribedConsumption, economy
            );

            await DisplayAlert("Успех", "Расчет завершен!\nРезультаты отображены в таблице", "OK");

        }
        catch (FormatException)
        {
            await DisplayAlert("Ошибка", "Проверьте правильность числовых значений", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
        }
    }

    private void UpdateResultTable(double groundMin, double airMin, double mainDensity,
                                  double auxQtyKg, string auxDoc, int drainBeforeFlight,
                                  double fuelBeforeStart, double consumedInFlight,
                                  double mainQtyKg, string mainDoc, double prescribedConsumption, double economy)
    {
        // Обновляем данные в таблице
        GroundLabel.Text = groundMin.ToString("F0");
        AirLabel.Text = airMin.ToString("F0");
        VSULabel.Text = mainDensity.ToString("F3");
        BeforeStartLabel.Text = fuelBeforeStart.ToString("F1");
        ConsumedLabel.Text = consumedInFlight.ToString("F1");
        RefueledAfterLabel.Text = mainQtyKg.ToString("F1");
        RefueledAfterDocLabel.Text = mainDoc;
        PrescribedLabel.Text = prescribedConsumption.ToString("F1");
        EconomyLabel.Text = economy.ToString("F1");

        // Заправлено перед вылетом
        if (AuxYesRadio.IsChecked)
        {
            RefueledBeforeLabel.Text = auxQtyKg.ToString("F1");
            RefueledDocLabel.Text = auxDoc;
        }
        else
        {
            RefueledBeforeLabel.Text = "-";
            RefueledDocLabel.Text = "-";
        }

        DrainedBeforeLabel.Text = drainBeforeFlight.ToString();
    }

    private double GetTimeInMinutes(string timeText)
    {
        if (string.IsNullOrEmpty(timeText)) return 0;

        if (timeText.Contains("-") || timeText.Contains(":"))
        {
            char separator = timeText.Contains("-") ? '-' : ':';
            string[] parts = timeText.Split(separator);

            if (parts.Length == 2 && int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
            {
                return hours * 60 + minutes;
            }
        }

        if (double.TryParse(timeText, out double minutesOnly))
            return minutesOnly;

        return -1; // Ошибка формата
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        // Очищаем поля ввода
        GroundTimeEntry.Text = "";
        AirTimeEntry.Text = "";
        AuxNoRadio.IsChecked = true;
        AuxYesRadio.IsChecked = false;
        AuxQtyEntry.Text = "";
        AuxDensityEntry.Text = "";
        AuxDocEntry.Text = "";
        MainQtyEntry.Text = "";
        MainDensityEntry.Text = "";
        MainDocEntry.Text = "";
        AuxInputLayout.IsVisible = false;

        // Сбрасываем таблицу
        ClearTable();
    }

    private void ClearTable()
    {
        DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy");
        DocLabel.Text = "";
        RouteLabel.Text = "";
        ExerciseLabel.Text = "";
        GroundLabel.Text = "";
        AirLabel.Text = "";
        VSULabel.Text = "";
        BeforeFlightLabel.Text = INITIAL_FUEL.ToString();
        RefueledBeforeLabel.Text = "";
        RefueledDocLabel.Text = "";
        DrainedBeforeLabel.Text = "";
        DrainedDocLabel.Text = "-";
        BeforeStartLabel.Text = "";
        ConsumedLabel.Text = "";
        RefueledAfterLabel.Text = "";
        RefueledAfterDocLabel.Text = "";
        AfterFlightLabel.Text = FINAL_FUEL.ToString();
        PrescribedLabel.Text = "";
        EconomyLabel.Text = "";
    }
}