// Copyright (c) Microsoft. All rights reserved.

pub(crate) struct Route<M>
where
    M: edgelet_core::ModuleRuntime + Send + Sync,
{
    runtime: std::sync::Arc<futures_util::lock::Mutex<M>>,

    since: Option<String>,
    until: Option<String>,
    iothub_hostname: Option<String>,
    edge_only: Option<String>,
}

#[async_trait::async_trait]
impl<M> http_common::server::Route for Route<M>
where
    M: edgelet_core::ModuleRuntime + Send + Sync,
{
    type ApiVersion = edgelet_http::ApiVersion;
    fn api_version() -> &'static dyn http_common::DynRangeBounds<Self::ApiVersion> {
        &((edgelet_http::ApiVersion::V2020_07_07)..)
    }

    type Service = crate::Service<M>;
    fn from_uri(
        service: &Self::Service,
        path: &str,
        query: &[(std::borrow::Cow<'_, str>, std::borrow::Cow<'_, str>)],
        _extensions: &http::Extensions,
    ) -> Option<Self> {
        if path != "/systeminfo/supportbundle" {
            return None;
        }

        let since = edgelet_http::find_query("since", query);
        let until = edgelet_http::find_query("until", query);
        let iothub_hostname = edgelet_http::find_query("iothub_hostname", query);
        let edge_only = edgelet_http::find_query("edge_runtime_only", query);

        Some(Route {
            runtime: service.runtime.clone(),

            since,
            until,
            iothub_hostname,
            edge_only,
        })
    }

    type DeleteBody = serde::de::IgnoredAny;
    type DeleteResponse = ();

    type GetResponse = (); // TODO: change type to logs type
    async fn get(self) -> http_common::server::RouteResponse<Self::GetResponse> {
        let log_options = self.log_options()?;

        let edge_only = if let Some(edge_only) = &self.edge_only {
            std::str::FromStr::from_str(edge_only).map_err(|err| {
                edgelet_http::error::bad_request("invalid parameter: edge_runtime_only")
            })?
        } else {
            false
        };

        let runtime = self.runtime.lock().await;

        todo!()
    }

    type PostBody = serde::de::IgnoredAny;
    type PostResponse = ();

    type PutBody = serde::de::IgnoredAny;
    type PutResponse = ();
}

impl<M> Route<M>
where
    M: edgelet_core::ModuleRuntime + Send + Sync,
{
    fn log_options(&self) -> Result<edgelet_core::LogOptions, http_common::server::Error> {
        let mut log_options = edgelet_core::LogOptions::new();

        if let Some(since) = &self.since {
            let since = edgelet_core::parse_since(&since)
                .map_err(|err| edgelet_http::error::bad_request("invalid parameter: since"))?;

            log_options = log_options.with_since(since);
        }

        if let Some(until) = &self.until {
            let until = edgelet_core::parse_since(&until)
                .map_err(|err| edgelet_http::error::bad_request("invalid parameter: until"))?;

            log_options = log_options.with_until(until);
        }

        Ok(log_options)
    }
}
