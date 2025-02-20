// Copyright (c) Microsoft. All rights reserved.

use std::env;
use std::io::Write;

use edgelet_utils::log_failure;
use log::{Level, LevelFilter};

const ENV_LOG: &str = "IOTEDGE_LOG";

fn syslog_level(level: Level) -> i8 {
    match level {
        Level::Error => 3,
        Level::Warn => 4,
        Level::Info => 6,
        Level::Debug | Level::Trace => 7,
    }
}

pub fn init_logger() {
    env_logger::Builder::new()
        .format(|fmt, record| {
            let level = match record.level() {
                Level::Trace => "TRCE",
                Level::Debug => "DBUG",
                Level::Info => "INFO",
                Level::Warn => "WARN",
                Level::Error => "ERR!",
            };
            let timestamp = fmt.timestamp();

            if record.level() >= Level::Debug {
                writeln!(
                    fmt,
                    "<{}>{} [{}] - [{}] {}",
                    syslog_level(record.level()),
                    timestamp,
                    level,
                    record.target(),
                    record.args()
                )
            } else {
                writeln!(
                    fmt,
                    "<{}>{} [{}] - {}",
                    syslog_level(record.level()),
                    timestamp,
                    level,
                    record.args()
                )
            }
        })
        .filter_level(LevelFilter::Info)
        .parse_filters(&env::var(ENV_LOG).unwrap_or_default())
        .init();
}

pub fn log_error(error: &dyn std::error::Error) {
    log_failure(Level::Error, error);
}
