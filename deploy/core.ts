import * as k8s from "@pulumi/kubernetes";

export const team_name = "dca";
export const namespace_name = "dca";
export const image_registry = "ghcr.io/dcarea";
export const service_name = "fabron";

export const shared_labels = {
    team: team_name,
    service: service_name
}

export const app_name_api = "fabron";
export const image_name_api = "fabron";
export const image_repo_api = `${image_registry}/${image_name_api}`;

// export const namespace = k8s.core.v1.Namespace.get("dca", "namespace/dca");
