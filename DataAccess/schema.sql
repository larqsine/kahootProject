drop schema if exists kahoot cascade;
create schema if not exists kahoot;

create table kahoot.game
(
    id   text primary key,
    name text not null
);

create table kahoot.question
(
    id            text primary key,
    game_id       text references kahoot.game (id),
    question_text text    not null,
    answered      boolean not null default false
);

create table kahoot.question_option
(
    id          text primary key,
    question_id text references kahoot.question (id),
    option_text text    not null,
    is_correct  boolean not null
);

create table kahoot.player
(
    id       text primary key,
    game_id  text references kahoot.game (id),
    nickname text not null,
    constraint unique_nickname_per_game unique (game_id, nickname)
);

create table kahoot.player_answer
(
    player_id          text references kahoot.player (id),
    question_id        text references kahoot.question (id),
    selected_option_id text references kahoot.question_option (id),
    answer_timestamp   timestamp with time zone,
    primary key (player_id, question_id)
);