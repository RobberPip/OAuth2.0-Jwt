# OAuth2.0-Jwt - Аутентификация на JWT токенах

## Описание

Основная идея сервиса заключается в предоставлении функционала авторизации пользователей на сайте с возможностью загрузки всех необходимых персональных данных. Для обеспечения безопасности при передаче данных между клиентом и сервером используется хеширование. Для контроля доступа генерируются и валидируются JWT-токены, которые подтверждают подлинность пользователя. Все персональные данные сохраняются в базе данных с соблюдением необходимых мер защиты.

Решение разделено на два микросервиса:
1. Сервис, отвечающий за логику Аутентификации
2. Сервис, отвечающий за работу с БД

## Предложения по улучшению
1. Добавить регистрацию
2. Изменить способ хэширования пароля
3. Добавить черный список токенов

## Решаемая задача

- Аутентификация пользователей

## Целевая аудитория

- Разработчики, занимающиеся разработкой веб-сайтов с возможностью авторизации/регистрации

## Используемые технологии
[![My Skills](https://skillicons.dev/icons?i=cs)](https://skillicons.dev)
1. C#
2. ASP.NET
3. Swagger
4. PostgreSQL
5. npgsql
6. Cryptography C#

## Основные функции

### Таблица HTTP запросов:

| Название запроса    | Тип запроса | Описание                                                                                                                                 |
|---------------------|-------------|------------------------------------------------------------------------------------------------------------------------------------------|
| **Logout**          | DELETE      | Удаляет сессию пользователя из БД, аннулирует `access` токен на клиенте и удаляет `refresh` токен в cookie.                                |
| **Login**           | POST        | Производит авторизацию пользователя, создает сессию в БД, генерирует `access` и `refresh` токены в HttpOnly Cookie.                     |
| **refreshToken**    | POST        | Обновляет `access` и `refresh` токены клиента. Используется в случае недействительности `access` токена.                                 |
| **getRefreshToken** | GET         | Выводит активный `refresh` токен пользователя.                                                                                           |
| **GetWeatherForecast** | GET (Authorize) | Тестовый запрос, необходим для получения данных. Требует авторизации. Используется для проверки авторизации.                          |

## Инструкция по развертыванию и запуску веб-сервиса

### 1. Подготовка БД
   - Установите PostgreSQL и настройте базу данных.
   - Примените SQL скрипты для создания структуры БД. Скрипты находятся в папке `db-scripts`.

### 2. Запуск сервисов
   - Запустите сервис `dbProvider`, который будет работать с базой данных.
   - Запустите сервис `AuthenticationService`, который отвечает за логику аутентификации.

### 3. Документация через Swagger
   - После запуска сервисов откройте Swagger UI по адресу: `http://localhost:port/swagger` для ознакомления с API и выполнения тестовых запросов.

## Примечания

- Все запросы, связанные с авторизацией, используют JWT токены для контроля доступа.
- Для обеспечения безопасности данные пользователей хешируются перед сохранением в базе данных.
