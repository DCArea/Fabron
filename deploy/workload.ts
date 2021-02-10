
import * as k8s from "@pulumi/kubernetes";
import * as kx from "@pulumi/kubernetesx";

const pb = new kx.PodBuilder({
    containers: [{
        image: "nginx",
        ports: {http: 80}, // Simplified ports syntax.
    }]
});
