import * as k8s from "@pulumi/kubernetes";
import { ServiceSpecType } from "@pulumi/kubernetes/core/v1";
import * as kx from "@pulumi/kubernetesx";
import { namespace, secret_dca_regcred } from "./core";

const image_version = process.env["IMAGE_VERSION"];
if (!image_version) { throw "missing IMAGE_VERSION" }
const pb = new kx.PodBuilder({
    imagePullSecrets: [secret_dca_regcred.metadata],
    containers: [{
        image: `ghcr.io/dcarea/fabron-service:${image_version}`,
        ports: { http: 80 },
        readinessProbe: {
            httpGet: {
                path: "/health",
                port: 80
            }
        }
    }],

});
export const deployment = new kx.Deployment("fabron-service", {
    metadata: {
        namespace: namespace.metadata.name,
        annotations: { "pulumi.com/skipAwait": "true" }
    },
    spec: pb.asDeploymentSpec({ replicas: 1 })
});


// export const service = new k8s.core.v1.Service("fabron-service", {
//     metadata: {
//         namespace: namespace.metadata.name,
//     },
//     spec: {
//         ports: [{ name: "http", port: 80 }],
//         selector: deployment.spec.template.metadata.labels,
//         type: ServiceSpecType.ClusterIP
//     }

// })

// export const ingress = new k8s.networking.v1.Ingress("fabron-service", {
//     metadata: {
//         namespace: namespace.metadata.name,
//     },
//     spec: {
//         ingressClassName: "nginx",
//         rules: [
//             {
//                 host: "fabron.doomed.app",
//                 http: {
//                     paths: [
//                         {
//                             path: "/",
//                             pathType: "Exact",
//                             backend: {
//                                 service: {
//                                     name: service.metadata.name,
//                                     port: { name: "http" },
//                                 }
//                             }
//                         },
//                     ],
//                 },
//             }
//         ]
//     }
// })

