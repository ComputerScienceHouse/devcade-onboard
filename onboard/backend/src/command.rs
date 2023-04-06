use serde::{Deserialize, Serialize};
use std::fmt::Display;
use std::process::ExitStatus;
use anyhow::Error;
use std::thread::JoinHandle;

use crate::api::{download_banner, download_game, download_icon, game_list, game_list_from_fs, launch_game};
use crate::api::schema::DevcadeGame;

/**
 * A request received by the backend from the frontend. The u32 is the request ID, which is used to
 * identify the request in the response to the frontend.
 */
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Request {
    Ping(u32), // Used to check if the backend is alive

    GetGameList(u32),
    GetGameListFromFs(u32),
    GetGame(u32, String),        // String is the game ID
    DownloadGame(u32, String),   // String is the game ID
    DownloadIcon(u32, String),   // String is the game ID
    DownloadBanner(u32, String), // String is the game ID

    SetProduction(u32, bool), // Sets prod / dev api url

    LaunchGame(u32, String), // String is the game ID
}

/**
 * A response sent by the backend to the frontend. The u32 is the request ID, which is used to
 * identify the request in the response to the frontend.
 */
#[derive(Debug, Serialize, Deserialize)]
pub enum Response {
    Pong(u32),

    Ok(u32),
    Err(u32, String),

    GameList(u32, Vec<DevcadeGame>),
    Game(u32, DevcadeGame),

    #[serde(skip)]
    InternalGame(u32, JoinHandle<ExitStatus>),
}

impl Response {
    /**
     * Create a new `Response::Ok` from a request ID.
     */
    fn ok_from_id(id: u32) -> Self {
        Response::Ok(id)
    }

    /**ew
     * Create a new `Response::Err` from a request ID and an error message.
     */
    fn err_from_id(id: u32, err: String) -> Self {
        Response::Err(id, err)
    }

    /**
     * Create a new `Response::Err` from a request ID and an error.
     */
    fn err_from_id_and_err(id: u32, err: Error) -> Self {
        Response::Err(id, err.to_string())
    }

    /**
     * Create a new `Response::GameList` from a request ID and a list of games.
     */
    fn game_list_from_id(id: u32, games: Vec<DevcadeGame>) -> Self {
        Response::GameList(id, games)
    }

    /**
     * Create a new `Response::Game` from a request ID and a game.
     */
    fn game_from_id(id: u32, game: DevcadeGame) -> Self {
        Response::Game(id, game)
    }

    /**
     * Create a new `Response::InternalGame` from a request ID and a game.
     */
    fn internal_game_from_id(id: u32, game: JoinHandle<ExitStatus>) -> Self {
        Response::InternalGame(id, game)
    }
}

/**
 * Handle a request from the frontend.
 */
pub async fn handle(req: Request) -> Response {
    match req {
        Request::Ping(id) => Response::Pong(id),
        Request::GetGameList(id) => match game_list().await {
            Ok(games) => Response::game_list_from_id(id, games),
            Err(err) => Response::err_from_id_and_err(id, err),
        },
        Request::GetGameListFromFs(id) => match game_list_from_fs() {
            Ok(games) => Response::game_list_from_id(id, games),
            Err(err) => Response::err_from_id_and_err(id, err),
        },
        Request::GetGame(id, game_id) => match game_list().await {
            Ok(game) => match game.into_iter().find(|g| g.id == game_id) {
                Some(game) => Response::game_from_id(id, game),
                None => Response::err_from_id(id, format!("Game with ID {game_id} not found")),
            },
            Err(err) => Response::err_from_id_and_err(id, err),
        },
        Request::DownloadGame(id, game_id) => match download_game(game_id).await {
            Ok(_) => Response::ok_from_id(id),
            Err(err) => Response::err_from_id_and_err(id, err),
        },
        Request::DownloadIcon(id, game_id) => match download_icon(game_id).await {
            Ok(_) => Response::ok_from_id(id),
            Err(err) => Response::err_from_id_and_err(id, err),
        },
        Request::DownloadBanner(id, game_id) => match download_banner(game_id).await {
            Ok(_) => Response::ok_from_id(id),
            Err(err) => Response::err_from_id_and_err(id, err),
        },
        Request::LaunchGame(id, game_id) => match launch_game(game_id).await {
            Ok(handle) => Response::internal_game_from_id(id, handle),
            Err(err) => Response::err_from_id_and_err(id, err),
        }
        Request::SetProduction(id, prod) => {
            crate::env::set_production(prod);
            Response::ok_from_id(id)
        }
    }
}

impl Display for Request {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Request::Ping(id) => write!(f, "[{id:9}] Ping"),
            Request::GetGameList(id) => write!(f, "[{id:9}] Get Game List"),
            Request::GetGameListFromFs(id) => write!(f, "[{id:9}] Get Game List From Filesystem"),
            Request::GetGame(id, game_id) => {
                write!(f, "[{id:9}] Get Game object with id ({game_id})")
            }
            Request::DownloadGame(id, game_id) => {
                write!(f, "[{id:9}] Download game with id ({game_id})")
            }
            Request::DownloadIcon(id, game_id) => {
                write!(f, "[{id:9}] Download icon with id ({game_id})")
            }
            Request::DownloadBanner(id, game_id) => {
                write!(f, "[{id:9}] Download banner with id ({game_id})")
            }
            Request::LaunchGame(id, game_id) => {
                write!(f, "[{id:9}] Launch game with id ({game_id})")
            }
            Request::SetProduction(id, prod) => {
                write!(f, "[{:9}] Set API to {}", id, if *prod { "production" } else { "development" })
            }
        }
    }
}

// Used for debug logging
impl Display for Response {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Response::Pong(id) => write!(f, "[{id:9}] Pong"),
            Response::Ok(id) => write!(f, "[{id:9}] Ok"),
            Response::Err(id, err) => write!(f, "[{id:9}] Err: {err}"),
            Response::GameList(id, games) => {
                write!(f, "[{:9}] Got game list with {} games", id, games.len())
            }
            Response::Game(id, game) => write!(f, "[{:9}] Downloaded game with id {}", id, game.id),
            Response::InternalGame(id, _) => write!(f, "[{id:9}] Launched game"),
        }
    }
}

