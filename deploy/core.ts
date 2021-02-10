import * as pulumi from "@pulumi/pulumi";
import * as k8s from "@pulumi/kubernetes";
import * as kx from "@pulumi/kubernetesx";
const config = new pulumi.Config();

export const namespace = k8s.core.v1.Namespace.get("fabron", "namespace/fabron");

const cr_pat = process.env["CR_PAT"];
if (!cr_pat) { throw "missing CR_PAT"}
export const secret_dca_regcred = new k8s.core.v1.Secret("dcaregcred", {
    metadata: {
        namespace: namespace.metadata.name
    },
    type: "kubernetes.io/dockerconfigjson",
    stringData: {
        ".dockerconfigjson": JSON.stringify({
            auths: {
                "ghcr.io": {
                    username: 'fabron',
                    password: cr_pat
                }
            }
        }),
    }
});
