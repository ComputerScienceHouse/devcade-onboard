use backend::env::devcade_path;
use backend::nfc::NFC_CLIENT;
use backend::servers::path::{game_pipe, onboard_pipe};
use backend::servers::ThreadHandles;
use log::{log, Level};
use tokio::fs;

#[tokio::main]
async fn main() -> ! {
    #[cfg(not(target_os = "linux"))]
    {
        compile_error!("This project only supports Linux.\nTo build for linux, run `cargo build --target x86_64-unknown-linux-gnu`");
    }

    match dotenvy::from_filename("../.env") {
        Ok(_) => (),
        Err(e) => {
            log!(Level::Error, "Error loading .env file: {}", e);
        }
    }
    env_logger::init();

    fs::create_dir_all(devcade_path())
        .await
        .expect("Couldn't create devcade dir");

    let mut handles: ThreadHandles = ThreadHandles::new();

    handles.restart_onboard(onboard_pipe());

    handles.restart_game(game_pipe());

    // Main loop
    loop {
        tokio::time::sleep(tokio::time::Duration::from_millis(1000)).await;
        // Check if any of the handles have finished
        if let Some(err) = handles.onboard_error() {
            log!(Level::Error, "Onboard thread has panicked: {}", err);
            handles.restart_onboard(onboard_pipe());
        }
        if let Some(err) = handles.game_error() {
            log!(Level::Error, "Game thread has panicked: {}", err);
            handles.restart_game(game_pipe());
        }
        if let Some(err) = NFC_CLIENT.nfc_error() {
            log!(Level::Error, "Gatekeeper thread has panicked: {:?}", err);
            NFC_CLIENT.restart();
        }
    }
}
