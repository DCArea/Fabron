import * as pulumi from "@pulumi/pulumi";
import * as k8s from "@pulumi/kubernetes";
import * as kx from "@pulumi/kubernetesx";
const config = new pulumi.Config();

export const namespace_fabron = new k8s.core.v1.Namespace("fabron");

export const secret_dca_regcred = new k8s.core.v1.Secret("dcaregcred", {
    metadata: {
        namespace: namespace_fabron.metadata.name
    },
    type: "kubernetes.io/dockerconfigjson",
    stringData: {
        ".dockerconfigjson": config
            .requireSecret("ghcr_dca_password")
            .apply(value => {
                return JSON.stringify({
                    auths: {
                        "ghcr.io": {
                            username: 'fabron',
                            password: value
                        }
                    }
                })
            })
    },
});
