import * as k8s from "@pulumi/kubernetes";
import * as kx from "@pulumi/kubernetesx";
import { namespace } from "./core";

const image_version = process.env["IMAGE_VERSION"];
if (!image_version) { throw "missing IMAGE_VERSION" }
const pb = new kx.PodBuilder({
    containers: [{
        image: `ghcr.io/dcarea/fabron-service:${image_version}`,
        ports: { http: 80 },
    }],
});
export const deployment = new kx.Deployment("fabron-service", {
    metadata: {
        namespace: namespace.metadata.name
    },
    spec: pb.asDeploymentSpec({ replicas: 1 })
});
export const service = deployment.createService({
    type: kx.types.ServiceType.LoadBalancer
});
