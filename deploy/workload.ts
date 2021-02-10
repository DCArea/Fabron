import * as k8s from "@pulumi/kubernetes";
import * as kx from "@pulumi/kubernetesx";
import { namespace } from "./core";

const pb = new kx.PodBuilder({
    containers: [{
        image: "nginx",
        ports: { http: 80 }, // Simplified ports syntax.
    }],
});
const deployment = new kx.Deployment("fabron-service", {
    metadata: {
        namespace: namespace.metadata.name
    },
    spec: pb.asDeploymentSpec({ replicas: 1 })
});
