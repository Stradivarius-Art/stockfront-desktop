# stockfront-desktop

Складской учёт с витриной (WPF, .NET 10, MySQL + EF Core).

После успешного входа открывается **дашборд складского учёта** (карточки-метрики + таблица товаров).
Разделы «Склад», «Витрина», «Заказы», «Отчёты» пока заглушки.

---

## Требования

- **.NET 10 SDK** (`dotnet --version` → 10.x).
- **Visual Studio 2022** (17.x) с рабочей нагрузкой **«Разработка классических приложений .NET»** (WPF).
- **MySQL 8.x**, запущенный и доступный.

Проект WPF (`net10.0-windows`), поэтому собирается и запускается на **Windows**.

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

Строка подключения и флаги читаются из файла `.env` в корне репозитория (он git-ignored).
Скопируйте шаблон и впишите свои данные:

```powershell
# из корня репозитория (PowerShell)
Copy-Item .env.example .env
```

Откройте `.env` и укажите доступ к MySQL:

```dotenv
STOCKFRONT_DB_CONNECTION=server=localhost;port=3306;database=stockfront;user=root;password=ВАШ_ПАРОЛЬ

# 1 — наполнить пустую БД демо-данными при первом запуске; 0 — не наполнять
STOCKFRONT_SEED_TESTDATA=0
```

## 3. Миграции (создание таблиц)

Есть два пути — достаточно любого.

**Вариант А (проще):** ничего не делать. При старте приложение само применяет миграции
(`Database.Migrate()`), создаёт таблицы и при пустой таблице пользователей заводит администратора
по умолчанию.

**Вариант Б (вручную, через EF CLI):**

```powershell
# один раз — установить инструмент EF Core
dotnet tool install --global dotnet-ef

dotnet restore
dotnet ef database update --project StockFront.Data
```

Строку подключения для миграций EF берёт из того же `.env` (через design-time фабрику
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

1. В `.env` выставьте `STOCKFRONT_SEED_TESTDATA=1`.
2. Запустите приложение (см. шаг 4).

Сидер **идемпотентен**: если товары в базе уже есть, он ничего не добавляет. Чтобы засеять заново —
очистите таблицы (или пересоздайте БД из шага 1) и запустите снова. После заполнения верните
`STOCKFRONT_SEED_TESTDATA=0`. Логика: `StockFront.Data/Seeding/TestDataSeeder.cs`.

### Вход

После запуска появится окно входа. Учётная запись администратора по умолчанию
(создаётся только в пустой БД):

| Логин   | Пароль     |
|---------|------------|
| `admin` | `admin123` |

Смените пароль после первого входа. Новых пользователей (роль «Покупатель») можно завести через
экран регистрации.

---

## Авторизация

- Модель 2-tier: WPF ↔ MySQL напрямую, без токенов/JWT. Вход = проверка пароля по
  хешу (BCrypt) + singleton-сессия `ICurrentUser` в памяти. См. скилл `stockfront-auth`.
- Роли: `Customer`, `WarehouseKeeper`, `Admin` (enum, хранится как `int`).
- Контракты — в `StockFront.Contracts` (`IAuthService`, `ICurrentUser`, `IUserRepository`),
  реализация — в `StockFront.Auth`, репозиторий — в `StockFront.Data`.

**Валидация:** правила в `CredentialRules` (Contracts) — логин 3–64 (латиница/цифры/`._-`),
пароль 6–72, e-mail опционален. Их применяет и экран регистрации (пофайлово), и `AuthService`
(чтобы правила держались в обход UI). Вход даёт общую ошибку без указания, что именно неверно.