create sequence sessions_id_seq;

alter sequence sessions_id_seq owner to postgres;

create table users
(
    uid      uuid    not null,
    id       integer not null
        constraint users_pk
            primary key,
    login    text    not null
        constraint users_pk_2
            unique,
    password text    not null,
    username text    not null,
    salt     text    not null
);

alter table users
    owner to postgres;

create table sessions
(
    user_id       integer                                                   not null
        constraint sessions_users_id_fk
            references users,
    refresh_token uuid                                                      not null,
    expires_in    timestamp                                                 not null,
    id            integer default nextval('info.sessions_id_seq'::regclass) not null
        constraint sessions_pk
            primary key
);

alter table sessions
    owner to postgres;

create procedure "AddSession"(IN _user_uid uuid, IN _refresh_jti_token uuid, IN _expiration timestamp with time zone)
    language plpgsql
as
$$
DECLARE
user_id integer;
BEGIN
    SELECT id INTO user_id FROM  users WHERE uid = _user_uid;
    -- Добавляем запись в таблицу sessions
    INSERT INTO sessions (user_id, refresh_token, expires_in)
    VALUES (user_id, _refresh_jti_token, _expiration);
END;
$$;

alter procedure "AddSession"(uuid, uuid, timestamp with time zone) owner to postgres;

create function manage_sessions() returns trigger
    language plpgsql
as
$$
BEGIN
    -- Проверяем количество сессий пользователя
    IF (SELECT COUNT(*) FROM sessions WHERE user_id = NEW.user_id) > 5 THEN
        -- Удаляем самую старую сессию
        DELETE FROM sessions
        WHERE id = (SELECT id FROM sessions WHERE user_id = NEW.user_id ORDER BY expires_in LIMIT 1);
    END IF;
    RETURN NEW;
END;
$$;

alter function manage_sessions() owner to postgres;

create trigger sessions_trigger
    after insert
    on sessions
    for each row
execute procedure manage_sessions();

create procedure "UpdateSession"(IN _user_uid uuid, IN _refresh_jti_token uuid, IN _expiration timestamp with time zone, IN _old_refresh_jti_token uuid)
    language plpgsql
as
$$
DECLARE
    _user_id integer;
BEGIN
    -- Получаем user_id по uid
    SELECT id INTO _user_id FROM users WHERE uid = _user_uid;

    -- Обновляем запись в таблице sessions
    UPDATE sessions
    SET refresh_token = _refresh_jti_token, expires_in = _expiration
    WHERE user_id = _user_id AND refresh_token = _old_refresh_jti_token;
END;
$$;

alter procedure "UpdateSession"(uuid, uuid, timestamp with time zone, uuid) owner to postgres;

create procedure "DeleteSession"(IN _user_uid uuid, IN _refresh_jti_token uuid)
    language plpgsql
as
$$
DECLARE
    _user_id INTEGER;
BEGIN
    -- Получаем user_id по uid
    SELECT id INTO _user_id FROM users WHERE uid = _user_uid;

    -- Удаляем запись из таблицы sessions
    DELETE FROM sessions
    WHERE user_id = _user_id AND refresh_token = _refresh_jti_token;
END;
$$;

alter procedure "DeleteSession"(uuid, uuid) owner to postgres;

