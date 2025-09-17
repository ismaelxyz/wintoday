declare module 'vue3-roulette' {
    import type { DefineComponent, VNodeProps, AllowedComponentProps, ComponentCustomProps } from 'vue';

    type RouletteProps = Record<string, unknown>;
    // DefineComponent<Props, RawBindings, Data>
    const Roulette: DefineComponent<RouletteProps, Record<string, unknown>, Record<string, unknown>> &
        Readonly<VNodeProps & AllowedComponentProps & ComponentCustomProps>;

    export { Roulette };
    export default Roulette;
}
