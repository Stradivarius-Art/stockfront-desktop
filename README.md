# stockfront-desktop

Складской учёт с витриной (WPF, .NET 9, MySQL + EF Core).

После успешного входа открывается **дашборд складского учёта** (карточки-метрики + таблица товаров).
Разделы «Склад», «Витрина», «Заказы», «Отчёты» пока заглушки.

---

## Требования

- **.NET 9 SDK** (`dotnet --version` → 9.x).
- **Visual Studio 2022** (17.x) с рабочей нагрузкой **«Разработка классических приложений .NET»** (WPF).
- **MySQL 8.x**, запущенный и доступный.

Проект WPF (`net9.0-windows`), поэтому собирается и запускается на **Windows**.

---

## Быстрый старт (с нуля до запущенного приложения)

```powershell
# 0. Проверка окружения
dotnet --version                # должно быть 9.x

# 1. Создать пустую БД (один раз)
mysql -u root -p -e "CREATE DATABASE stockfront CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# 2. Настроить подключение
Copy-Item .env.example .env     # затем впишите пароль MySQL в .env

# 3. Восстановить зависимости и собрать
dotnet restore
dotnet build -c Debug

# 4. Подготовить схему и администратора (один раз)
dotnet run --project stockfront.csproj -- --migrate --seed-admin

# 5. (необязательно) Наполнить демо-данными
dotnet run --project stockfront.csproj -- --seed

# 6. Запустить приложение
dotnet run --project stockfront.csproj
```

Вход по умолчанию — `admin` / `admin123` (см. раздел «Вход»).

Подробности по каждому шагу — ниже.

---

## 1. База данных

Создайте пустую базу (имя должно совпадать с тем, что в строке подключения, по умолчанию `stockfront`):

```sql
CREATE DATABASE stockfront CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

Например, одной командой из консоли:

```bash
mysql -u root -p -e "CREATE DATABASE stockfront CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
```

## 2. Конфигурация (.env)

Строка подключения читается из файла `.env` в корне репозитория (он git-ignored).
Скопируйте шаблон и впишите свои данные:

```powershell
# из корня репозитория (PowerShell)
Copy-Item .env.example .env
```

Откройте `.env` и укажите доступ к MySQL:

```dotenv
STOCKFRONT_DB_CONNECTION=server=localhost;port=3306;database=stockfront;user=root;password=ВАШ_ПАРОЛЬ
```

## 3. Миграции (создание таблиц)

> Приложение **не** применяет миграции и не пишет в БД при обычном запуске — схема готовится
> отдельными командами ниже, поэтому повторный запуск не затирает данные.

**Вариант А (через приложение):** применить миграции и завести администратора по умолчанию
на пустой БД:

```powershell
dotnet run --project stockfront.csproj -- --migrate     # создать/обновить таблицы
dotnet run --project stockfront.csproj -- --seed-admin  # администратор по умолчанию (только в пустой БД)

# то же одной командой (порядок: схема → админ):
dotnet run --project stockfront.csproj -- --migrate --seed-admin
```

**Вариант Б (через EF CLI):**

```powershell
# один раз — установить инструмент EF Core
dotnet tool install --global dotnet-ef

dotnet restore
dotnet ef database update --project StockFront.Data
```

Строку подключения обе команды берут из того же `.env` (через design-time фабрику
`AppDbContextFactory`).

## 4. Сборка и запуск

### Через Visual Studio

1. Открыть `StockFront.sln`.
2. Стартовый проект — **`stockfront`** (ПКМ по проекту → *Set as Startup Project*).
3. Запустить: **F5** (с отладкой) или **Ctrl+F5** (без отладки).

### Через консоль (.NET CLI)

```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project stockfront.csproj
```

## 5. Заполнение тестовыми данными

Сидер создаёт категории, товары с остатками (включая позиции из макета) и несколько заказов.
Запускается отдельной командой (схема должна быть уже создана — см. шаг 3):

```powershell
dotnet run --project stockfront.csproj -- --seed
```

Сидер **идемпотентен**: если товары в базе уже есть, он ничего не добавляет. Чтобы засеять заново —
очистите таблицы (или пересоздайте БД из шага 1) и запустите команду снова. Логика:
`StockFront.Data/Seeding/TestDataSeeder.cs`.

### Вход

После запуска появится окно входа. Вход выполняется по **e-mail и паролю**. Учётная запись
администратора по умолчанию (создаётся командой `--seed-admin` только в пустой БД):

| E-mail                  | Пароль     |
|-------------------------|------------|
| `admin@stockfront.local`| `admin123` |

Смените пароль после первого входа. Новых пользователей (роль «Покупатель») можно завести через
экран регистрации.

## 6. Проверка, что всё работает

Пройдитесь по шагам — каждый должен завершиться без ошибок:

1. **SDK той версии:** `dotnet --version` выводит `9.x`.
2. **Сборка:** `dotnet build -c Debug` — `Сборка успешно завершена`, 0 ошибок.
3. **БД доступна:** команда `-- --migrate` отрабатывает без ошибок подключения; в MySQL появляются
   таблицы (`SHOW TABLES FROM stockfront;`).
4. **Администратор заведён:** после `-- --seed-admin` в таблице пользователей есть запись админа
   (`SELECT Email FROM stockfront.Users;`).
5. **Запуск:** `dotnet run --project stockfront.csproj` открывает окно входа; под
   `admin@stockfront.local`/`admin123` открывается дашборд с карточками-метриками.
6. **Демо-данные (если сеяли):** на дашборде видны товары и заказы из `TestDataSeeder`.

---