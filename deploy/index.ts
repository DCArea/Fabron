import * as workloads from "./workloads";

const { deployment } = workloads.deploy();

export const image = deployment.spec.template.spec.containers[0].image;
