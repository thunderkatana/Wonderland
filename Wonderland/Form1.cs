using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media; // Для работы со звуками

namespace Wonderland
{
    public partial class Form1 : Form
    {
        Random random = new Random();
        int totalScore = 0;
        string answer;
        bool isBaraban = false;

        // Переменные для анимации колеса
        private float wheelAngle = 0; // Угол поворота колеса
        private float wheelSpeed = 20; // Скорость вращения
        private float deceleration = 0.5f; // Замедление
        private bool isSpinning = false; // Флаг вращения
        private int finalScore = 0; // Итоговое количество очков после остановки

        // Секторы колеса (пример: 8 секторов с разными очками)
        private readonly int[] wheelScores = { 100, 200, 300, 500, 700, 1000, 1500, 0 };
        private readonly Color[] sectorColors = { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Orange, Color.Purple, Color.Cyan, Color.Gray };

        // Звуковые эффекты
        private SoundPlayer spinSound;
        private SoundPlayer winSound;
        private SoundPlayer correctSound;
        private SoundPlayer wrongSound;

        public Form1()
        {
            InitializeComponent();
            timerWheel.Interval = 30; // Интервал таймера (в миллисекундах)
            timerWheel.Tick += TimerWheel_Tick;

            // Подключаем событие Load вручную
            this.Load += new EventHandler(Form1_Load);

            // Инициализация звуков
            InitializeSounds();
        }

        private void InitializeSounds()
        {
            try
            {
                spinSound = new SoundPlayer("Sounds/spin.wav"); // Звук вращения колеса
                winSound = new SoundPlayer("Sounds/win.wav"); // Звук победы
                correctSound = new SoundPlayer("Sounds/correct.wav"); // Звук правильной буквы
                wrongSound = new SoundPlayer("Sounds/wrong.wav"); // Звук неправильной буквы
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке звуков: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Вопросы
            int WordsCount = 3;
            string[] questions = new string[WordsCount];
            questions[0] = "2+2*2";
            questions[1] = "5+1*2";
            questions[2] = "3+3*2";

            // Ответы
            string[] answers = new string[WordsCount];
            answers[0] = "ШЕСТЬ";
            answers[1] = "СЕМЬ";
            answers[2] = "ДЕВЯТЬ";

            // Выбор случайного вопроса
            var RandomQuestionIndex = random.Next(questions.Length);
            // Вывод в поле lblQuestion
            lblQuestion.Text = questions[RandomQuestionIndex];

            answer = answers[RandomQuestionIndex];
            string word = "";
            for (int i = 0; i < answer.Length; i++)
            {
                word += "*";
            }

            lblSecretWord.Text = word;

            // Убедитесь, что PictureBox настроен для перерисовки
            pictureBoxWheel.Paint += pictureBoxWheel_Paint;
        }

        private void btnSpin_Click(object sender, EventArgs e)
        {
            if (!isSpinning)
            {
                // Запуск анимации вращения
                isSpinning = true;
                wheelSpeed = 20; // Начальная скорость вращения
                timerWheel.Start();

                // Проигрываем звук вращения
                spinSound.PlayLooping(); // Воспроизводим звук в цикле

                // Сбрасываем текущий результат
                lblSpinScore.Text = "Вращается...";
            }
        }

        private void TimerWheel_Tick(object sender, EventArgs e)
        {
            // Обновляем угол поворота
            wheelAngle += wheelSpeed;

            // Замедляем вращение
            wheelSpeed -= deceleration;

            // Если скорость упала до нуля, останавливаем вращение
            if (wheelSpeed <= 0)
            {
                wheelSpeed = 0;
                isSpinning = false;
                timerWheel.Stop();

                // Останавливаем звук вращения
                spinSound.Stop();

                // Определяем результат вращения
                DetermineWheelResult();
            }

            // Перерисовываем колесо
            pictureBoxWheel.Invalidate();
        }

        private void pictureBoxWheel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // Сглаживание для красивого отображения

            // Центрируем колесо
            float centerX = pictureBoxWheel.Width / 2f;
            float centerY = pictureBoxWheel.Height / 2f;
            float radius = Math.Min(centerX, centerY) - 10; // Радиус колеса с небольшим отступом

            // Поворачиваем колесо
            e.Graphics.TranslateTransform(centerX, centerY);
            e.Graphics.RotateTransform(wheelAngle);

            // Рисуем секторы
            int sectorCount = wheelScores.Length;
            float sectorAngle = 360f / sectorCount; // Угол одного сектора

            for (int i = 0; i < sectorCount; i++)
            {
                // Рисуем сектор
                using (SolidBrush brush = new SolidBrush(sectorColors[i]))
                {
                    e.Graphics.FillPie(brush, -radius, -radius, radius * 2, radius * 2, i * sectorAngle, sectorAngle);
                }

                // Рисуем границы секторов
                using (Pen pen = new Pen(Color.Black, 2))
                {
                    e.Graphics.DrawPie(pen, -radius, -radius, radius * 2, radius * 2, i * sectorAngle, sectorAngle);
                }
            }

            // Сбрасываем трансформацию после рисования секторов
            e.Graphics.ResetTransform();
            e.Graphics.TranslateTransform(centerX, centerY);

            // Рисуем центральный круг (для красоты)
            using (SolidBrush centerBrush = new SolidBrush(Color.White))
            {
                float centerRadius = radius * 0.15f; // Уменьшаем радиус центрального круга
                e.Graphics.FillEllipse(centerBrush, -centerRadius, -centerRadius, centerRadius * 2, centerRadius * 2);
            }

            // Сбрасываем трансформацию
            e.Graphics.ResetTransform();
        }

        private void DetermineWheelResult()
        {
            // Определяем, на каком секторе остановилось колесо
            int sectorCount = wheelScores.Length;
            float sectorAngle = 360f / sectorCount; // Угол одного сектора
            float normalizedAngle = wheelAngle % 360; // Нормализуем угол до 0-360
            if (normalizedAngle < 0) normalizedAngle += 360; // Устраняем отрицательные углы

            // Определяем индекс сектора
            int sectorIndex = (int)(normalizedAngle / sectorAngle);
            finalScore = wheelScores[sectorIndex];

            // Выводим результат
            lblSpinScore.Text = finalScore.ToString();

            // Обновляем общий счет
            totalScore += finalScore;
            label33.Text = totalScore.ToString();

            isBaraban = true; // Разрешаем угадывать буквы
        }

        private void label2_Click(object sender, EventArgs e)
        {
            if (!isBaraban)
            {
                MessageBox.Show("Сначала барабан!");
                return;
            }

            Label symbol = (Label)sender;
            // Обработка секретного слова, если в нем есть введенная буква
            string word = lblSecretWord.Text;
            string newWord = "";
            bool letterFound = false; // Флаг, чтобы проверить, была ли найдена буква

            for (int i = 0; i < word.Length; i++)
            {
                if (answer[i].ToString() == symbol.Text)
                {
                    newWord += answer[i];
                    letterFound = true;
                }
                else
                {
                    newWord += word[i];
                }
            }

            lblSecretWord.Text = newWord;

            // Сравнение угаданного слова с ответом
            if (newWord == answer)
            {
                winSound.Play(); // Проигрываем звук победы
                MessageBox.Show($"Победа! Ваш счет: {totalScore}");
            }
            else if (letterFound) // Если буква была найдена
            {
                correctSound.Play(); // Проигрываем звук правильной буквы
            }
            else // Если буква не была найдена
            {
                wrongSound.Play(); // Проигрываем звук неправильной буквы
                MessageBox.Show("Нет такой буквы!");
            }

            isBaraban = false; // Сбрасываем флаг, чтобы снова требовалось крутить барабан
        }

        private void label2_MouseEnter(object sender, EventArgs e)
        {
            ((Label)sender).Font = new Font("Comic Sans", 24F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        }

        private void label2_MouseLeave(object sender, EventArgs e)
        {
            ((Label)sender).Font = new Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        }
    }
}